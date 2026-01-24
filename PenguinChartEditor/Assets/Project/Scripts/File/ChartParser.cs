using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class ChartParser
{
    #region Metadata Constants

    private const string QUOTES_STRING = "\"";
    private const string YEAR_COMMA = ", ";
    private const int SONG_HEADER_LOCATION = 0;

    #endregion

    #region Sync Track Constants

    private const string HELPFUL_REMINDER = "Please check the file and try again.";
    private const string SPECIAL_INDICATOR = "S";
    public const int INDENTIFIER_INDEX = 0;

    #endregion

    #region Setup

    private static SyncTrackInstrument syncTrackInstrument;
    private static ConcurrentBag<RawStarpowerEvent> rawStarpowerEvents = new();
    private static Metadata metadata;
    private static ConcurrentBag<IInstrument> instruments = new();

    // note: chart resolution is set within FormatEventSections so that SyncTrack can use it via Chart.Resolution
    public static void ParseChart(string filePath)
    {
        syncTrackInstrument = null;
        metadata = null;
        instruments = new ConcurrentBag<IInstrument>();

        string[] chartAsLines = File.ReadAllLines(filePath);
        var eventGroups = FormatEventSections(chartAsLines);

        Parallel.ForEach(eventGroups, item => ProcessEventGroup(item));

        var starpower = new StarpowerInstrument(rawStarpowerEvents.ToList());

        Chart.ApplyFileInformation(
            metadata,
            instruments.ToList(),
            syncTrackInstrument,
            starpower
            );
    }

    private static void ProcessEventGroup(ChartEventGroup eventGroup)
    {
        if (eventGroup == null) return;
        switch (eventGroup.InstrumentID)
        {
            case HeaderType.SyncTrack: // required (needs exception handling)
                syncTrackInstrument = new SyncTrackInstrument(eventGroup.data);
                break;
            case HeaderType.Events:
                // events parse
                break;
            default:
                var instrument = ParseInstrumentGroup(eventGroup);
                if (instrument != null) instruments.Add(instrument);
                break;
        }
    }

    #endregion

    #region Event Section Setup

    private static ConcurrentBag<ChartEventGroup> FormatEventSections(string[] chartAsLines)
    {
        // "[" begins a section header => begins a section of interest to parse (details are validated later)
        List<int> sectionHeaderCandidates = Enumerable.Range(0, chartAsLines.Length).Where(i => chartAsLines[i].Contains("[")).ToList();

        if (sectionHeaderCandidates.Count == 0)
            throw new ArgumentException("Invalid chart file. There are no event blocks within the file!");


        // song data is processed seperately (string, string) vs (int, string) with all other sections
        // get it done first to get resolution handled => essential for sync track and others
        if (sectionHeaderCandidates.Contains(SONG_HEADER_LOCATION))
        {
            sectionHeaderCandidates.Remove(SONG_HEADER_LOCATION);

            var songData = InitializeSongGroup(SONG_HEADER_LOCATION, chartAsLines);
            Chart.Resolution = GetChartResolution(songData);
            metadata = ParseSongMetadata(songData);
        }

        ConcurrentBag<ChartEventGroup> identifiedSections = new();

        Parallel.ForEach(sectionHeaderCandidates, item => identifiedSections.Add(InitializeEventGroup(item, chartAsLines)));

        return identifiedSections;
    }

    /// <summary>
    /// Make a new ChartEventGroup from the enclosed data in { ... } following a [SectionName].
    /// </summary>
    /// <param name="lineIndex">The index of the section header</param>
    /// <returns>ChartEventGroup with Identifier SectionName and data { ... }</returns>
    /// <exception cref="ArgumentException"></exception>
    private static ChartEventGroup InitializeEventGroup(int lineIndex, string[] chartAsLines)
    {
        var identifierLine = chartAsLines[lineIndex];

        if (!InstrumentMetadata.TryParseHeaderType(identifierLine, out var sectionHeader))
        {
            return null;
        }

        ChartEventGroup identifiedSection = new(sectionHeader);

        if (chartAsLines[lineIndex + 1] != "{") // line with { to mark beginning of section
            throw new ArgumentException($"{identifiedSection.InstrumentID} is not enclosed properly. {HELPFUL_REMINDER}");

        lineIndex += 2; // line with first bit of data

        List<KeyValuePair<int, string>> eventData = new();
        string workingLine = chartAsLines[lineIndex];

        while (workingLine != "}" && lineIndex < chartAsLines.Length - 1)
        {
            if (!InstrumentMetadata.TryParseChartLine(workingLine, out var formattedKVP))
            {
                continue;
            }

            if (TryGetStarpowerEvent(formattedKVP, identifiedSection.InstrumentID, out RawStarpowerEvent @event))
            {
                rawStarpowerEvents.Add(@event);
            }
            else
            {
                eventData.Add(formattedKVP);
            }

            lineIndex++;
            workingLine = chartAsLines[lineIndex];
        }

        identifiedSection.data = eventData;
        return identifiedSection;
    }

    private static RawStarpowerEvent defaultRawSPEvent = new((HeaderType)(-1), -1, "");

    private static bool TryGetStarpowerEvent(KeyValuePair<int, string> @event, HeaderType targetInstrument, out RawStarpowerEvent rawStarpowerEvent)
    {
        rawStarpowerEvent = defaultRawSPEvent;

        if (!@event.Value.Contains('S'))
        {
            return false;
        }

        var values = @event.Value.Split(' ');

        if (values[INDENTIFIER_INDEX] != SPECIAL_INDICATOR) return false;

        // Used to avoid drum S 65 and S 66 for drum rolls (and other future unimplemented events)
        if (!StarpowerInstrument.IsSpecialEventStarpowerEvent(values)) return false;

        rawStarpowerEvent = new RawStarpowerEvent(targetInstrument, @event.Key, @event.Value);
        return true;
    }

    /// <summary>
    /// Make a new ChartEventGroup from an ini file containing song metadata. 
    /// </summary>
    /// <param name="iniPath">File path of the .ini file.</param>
    /// <returns>ChartEventGroup with data from .ini file.</returns>
    private static SongDataGroup InitializeIniGroup(string iniPath)
    {
        var iniData = File.ReadAllLines(iniPath);

        List<KeyValuePair<string, string>> eventData = new();

        // index 0 is [Song] - not necessary
        for (int lineIndex = 1; lineIndex < iniData.Length; lineIndex++)
        {
            var lineParts = iniData[lineIndex].Split(" = ");
            if (lineParts.Length > 1)
                eventData.Add(new KeyValuePair<string, string>(lineParts[0].Trim(), lineParts[1].Trim()));
        }

        SongDataGroup iniGroup = new(eventData);

        return iniGroup;
    }

    private static SongDataGroup InitializeSongGroup(int lineIndex, string[] chartAsLines)
    {
        if (chartAsLines[lineIndex + 1] != "{") // line with { to mark beginning of section
            throw new ArgumentException($"[Song] is not enclosed properly. {HELPFUL_REMINDER}");

        lineIndex += 2; // line with first bit of data

        List<KeyValuePair<string, string>> eventData = new();
        string workingLine = chartAsLines[lineIndex];

        // this needs more exception handling (not properly enclosed sections, etc.)
        while (workingLine != "}" && lineIndex < chartAsLines.Length - 1)
        {
            var parts = workingLine.Split(" = ", 2);
            eventData.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));

            lineIndex++;
            workingLine = chartAsLines[lineIndex];
        }

        return new SongDataGroup(eventData);
    }

    #endregion

    #region Metadata

    /// <summary>
    /// 
    /// </summary>
    /// <param name="songEventGroup">ChartEventGroup object with [Song] header.</param>
    /// <returns>Constructed Metadata, resolution</returns>
    /// <exception cref="ArgumentException"></exception>
    private static Metadata ParseSongMetadata(SongDataGroup songEventGroup)
    {
        Metadata metadata = new();
        if (File.Exists($"{Chart.FolderPath}/song.ini")) // read from ini if exists (most reliable scenario)
        {
            var iniEventGroup = InitializeIniGroup($"{Chart.FolderPath}/song.ini");

            foreach (var kvp in iniEventGroup.data)
            {
                if (Enum.TryParse(typeof(Metadata.MetadataType), kvp.Key, true, out var formattedKey))
                {
                    metadata.SongInfo.Add((Metadata.MetadataType)formattedKey, kvp.Value);
                }
                else if (Enum.TryParse(typeof(Metadata.InstrumentDifficultyIdentifier), kvp.Key, true, out var formattedInstrumentDiff))
                {
                    if (int.TryParse(kvp.Value, out int instrumentDifficulty))
                    {
                        metadata.Difficulties.Add((Metadata.InstrumentDifficultyIdentifier)formattedInstrumentDiff, instrumentDifficulty);
                    }
                }
                // log warning about unrecognized key
            }
        }
        else // read what we can from embedded .chart data
        {
            // log warning about ini being more efficient?

            foreach (var kvp in songEventGroup.data)
            {
                if (Enum.TryParse(typeof(Metadata.MetadataType), kvp.Key, true, out var iniFormattedKey))
                {
                    var formattedValue = kvp.Value.Replace(QUOTES_STRING, "").Replace(YEAR_COMMA, "");
                    metadata.SongInfo.Add((Metadata.MetadataType)iniFormattedKey, formattedValue);
                }
            }
        }

        return metadata;
    }

    private static int GetChartResolution(SongDataGroup songEventGroup)
    {
        var resolutionData = songEventGroup.data.Where(list => list.Key == "Resolution").ToList();

        if (resolutionData.Count == 0)
            throw new ArgumentException("No resolution data found within chart file. Please add the correct resolution and try again.");

        if (!int.TryParse(resolutionData[0].Value, out int resolutionValue))
            throw new ArgumentException($"Resolution data is not valid. {HELPFUL_REMINDER}");

        return resolutionValue;
    }

    #endregion

    #region Instruments

    private static IInstrument ParseInstrumentGroup(ChartEventGroup chartEventGroup)
    {
        switch (InstrumentMetadata.GetInstrumentGroup(chartEventGroup.InstrumentID))
        {
            case InstrumentCategory.FiveFret:
                return new FiveFretInstrument(chartEventGroup.InstrumentID, chartEventGroup.data);
            case InstrumentCategory.FourLaneDrums:
                // parse drums
                break;
            case InstrumentCategory.GHL:
                // parse GHL
                break;
            case InstrumentCategory.Vox:
                // parse vox
                break;
        }
        //Chart.Log("Skipped instrument group");
        return null;
        // throw new ArgumentException("Tried to parse an unsupported instrument group.");
    }

    #endregion
}

#region ChartEventGroup construction

/// <summary>
/// Object that contains event section data (enclosed in [SectionName] { ... }) in deconstructed KeyValuePairs. 
/// </summary>
public class ChartEventGroup
{
    public HeaderType InstrumentID;
    public List<KeyValuePair<int, string>> data;

    public ChartEventGroup(HeaderType identifier)
    {
        InstrumentID = identifier;
    }
}

// ChartEventGroup but formatted for .ini and [Song] sections 
internal class SongDataGroup
{
    public List<KeyValuePair<string, string>> data;

    public SongDataGroup(List<KeyValuePair<string, string>> data)
    {
        this.data = data;
    }
}

public struct RawStarpowerEvent
{
    public HeaderType header;
    public int tick;
    public string data;

    public RawStarpowerEvent(HeaderType header, int tick, string data)
    {
        this.header = header;
        this.tick = tick;
        this.data = data;
    }
}

#endregion