using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ChartParser
{
    #region Metadata Constants

    const string QUOTES_STRING = "\"";
    const string YEAR_COMMA = ", ";
    const int SONG_HEADER_LOCATION = 0;

    #endregion

    #region Sync Track Constants
    const string HELPFUL_REMINDER = "Please check the file and try again.";

    // Note: When creating new chart file, make sure [SyncTrack] starts with a BPM and TS declaration!
    const int DEFAULT_TS_DENOMINATOR = 4;
    const string TEMPO_EVENT_INDICATOR = "B";
    const string TIME_SIGNATURE_EVENT_INDICATOR = "TS";
    const string ANCHOR_INDICATOR = "A";
    const string SYNC_TRACK_ERROR = "[SyncTrack] has invalid tempo event:";
    const float BPM_FORMAT_CONVERSION = 1000.0f;
    const int TS_POWER_CONVERSION_NUMBER = 2;
    const float SECONDS_PER_MINUTE = 60;

    #endregion

    #region Accessors 
    public SyncTrackInstrument syncTrackInstrument;
    public Metadata metadata;
    public List<IInstrument> instruments = new();

    #endregion

    #region Setup

    public ChartParser(string filePath)
    {
        chartAsLines = File.ReadAllLines(filePath);
        ParseChartData();
    }
    string[] chartAsLines;

    void ParseChartData()
    {
        var eventGroups = FormatEventSections();

        Parallel.ForEach(eventGroups, item => ProcessEventGroup(item));
    }

    void ProcessEventGroup(ChartEventGroup eventGroup)
    {
        if (eventGroup == null) return;
        switch (eventGroup.EventGroupIdentifier)
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

    List<ChartEventGroup> FormatEventSections()
    {
        // "[" begins a section header => begins a section of interest to parse (details are validated later)
        List<int> sectionHeaderCandidates = Enumerable.Range(0, chartAsLines.Length).Where(i => chartAsLines[i].Contains("[")).ToList();

        if (sectionHeaderCandidates.Count == 0)
            throw new ArgumentException("Invalid chart file. There are no event blocks within the file!");

        List<ChartEventGroup> identifiedSections = new();

        // song data is processed seperately (string, string) vs (int, string) with all other sections
        // get it done first to get resolution handled => essential for sync track and others
        if (sectionHeaderCandidates.Contains(SONG_HEADER_LOCATION))
        {
            sectionHeaderCandidates.Remove(SONG_HEADER_LOCATION);

            var songData = InitializeSongGroup(SONG_HEADER_LOCATION);
            Chart.Resolution = GetChartResolution(songData);
            metadata = ParseSongMetadata(songData);
        }

        Parallel.ForEach(sectionHeaderCandidates, item => identifiedSections.Add(FormatEventSection(item)));

        return identifiedSections;
    }

    ChartEventGroup FormatEventSection(int lineIndex)
    {
        var eventGroup = InitializeEventGroup(lineIndex);
        if (eventGroup != null) return eventGroup;
        return null;
    }

    /// <summary>
    /// Make a new ChartEventGroup from the enclosed data in { ... } following a [SectionName].
    /// </summary>
    /// <param name="lineIndex">The index of the section header</param>
    /// <returns>ChartEventGroup with Identifier SectionName and data { ... }</returns>
    /// <exception cref="ArgumentException"></exception>
    ChartEventGroup InitializeEventGroup(int lineIndex)
    {
        var identifierLine = chartAsLines[lineIndex];
        var cleanIdentifier = identifierLine.Replace("[", "").Replace("]", "");

        if (!Enum.TryParse(typeof(HeaderType), cleanIdentifier, true, out var enumObject))
        {
            return null;
        }
        ChartEventGroup identifiedSection = new((HeaderType)enumObject);

        if (chartAsLines[lineIndex + 1] != "{") // line with { to mark beginning of section
            throw new ArgumentException($"{identifiedSection.EventGroupIdentifier} is not enclosed properly. {HELPFUL_REMINDER}");

        lineIndex += 2; // line with first bit of data

        List<KeyValuePair<int,string>> eventData = new();
        string workingLine = chartAsLines[lineIndex];

        while (workingLine != "}" && lineIndex < chartAsLines.Length - 1)
        {
            var parts = workingLine.Split(" = ", 2);
            if (!int.TryParse(parts[0].Trim(), out int tick))
            {
                throw new ArgumentException($"Problem parsing tick {parts[0].Trim()}");
            }

            eventData.Add(new(tick, parts[1]));

            lineIndex++;
            workingLine = chartAsLines[lineIndex];
        }

        identifiedSection.data = eventData;
        return identifiedSection;
    }

    /// <summary>
    /// Make a new ChartEventGroup from an ini file containing song metadata. 
    /// </summary>
    /// <param name="iniPath">File path of the .ini file.</param>
    /// <returns>ChartEventGroup with data from .ini file.</returns>
    SongDataGroup InitializeIniGroup(string iniPath)
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

    SongDataGroup InitializeSongGroup(int lineIndex)
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
            eventData.Add(new(parts[0].Trim(), parts[1].Trim()));

            lineIndex++;
            workingLine = chartAsLines[lineIndex];
        }

        return new(eventData);
    }

    #endregion

    #region Metadata

    /// <summary>
    /// 
    /// </summary>
    /// <param name="songEventGroup">ChartEventGroup object with [Song] header.</param>
    /// <returns>Constructed Metadata, resolution</returns>
    /// <exception cref="ArgumentException"></exception>
    Metadata ParseSongMetadata(SongDataGroup songEventGroup)
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

    int GetChartResolution(SongDataGroup songEventGroup)
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
    IInstrument ParseInstrumentGroup(ChartEventGroup chartEventGroup)
    {
        switch (chartEventGroup.GetInstrumentGroup())
        {
            case InstrumentCategory.FiveFret:
                return new FiveFretInstrument(chartEventGroup.EventGroupIdentifier, chartEventGroup.data);
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
class ChartEventGroup
{
    public InstrumentCategory GetInstrumentGroup()
    {
        return (int)EventGroupIdentifier switch
        {
            < 10 => InstrumentCategory.None,
            < 100 => InstrumentCategory.FiveFret,
            < 1000 => InstrumentCategory.FourLaneDrums,
            < 10000 => InstrumentCategory.GHL,
            < 100000 => InstrumentCategory.Vox,
            _ => throw new ArgumentException("Tried to get invalid instrument group.")
        };
    }

    public DifficultyType GetDifficulty() => GetDifficulty(EventGroupIdentifier);

    public static DifficultyType GetDifficulty(HeaderType instrumentID)
    {
        return ((int)instrumentID % 10) switch
        {
            0 => DifficultyType.easy,
            1 => DifficultyType.medium,
            2 => DifficultyType.hard,
            3 => DifficultyType.expert,
            _ => throw new ArgumentException("Tried to get invalid instrument difficulty.")
        };
    }

    public HeaderType EventGroupIdentifier;
    public List<KeyValuePair<int, string>> data;

    public ChartEventGroup(HeaderType identifier)
    {
        EventGroupIdentifier = identifier;
    }
}

// ChartEventGroup but formatted for .ini and [Song] sections 
class SongDataGroup
{
    public List<KeyValuePair<string, string>> data;

    public SongDataGroup(List<KeyValuePair<string, string>> data)
    {
        this.data = data;
    }
}

#endregion