using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class ChartParser
{
    // Note: When creating new chart file, make sure [SyncTrack] starts with a BPM and TS declaration!
    const int DEFAULT_TS_DENOMINATOR = 4;
    const string TEMPO_EVENT_INDICATOR = "B";
    const string TIME_SIGNATURE_EVENT_INDICATOR = "TS";
    const string HELPFUL_REMINDER = "Please check the file and try again.";
    const string SYNC_TRACK_ERROR = "[SyncTrack] has invalid tempo event:";
    const float BPM_FORMAT_CONVERSION = 1000.0f;
    const int TS_POWER_CONVERSION_NUMBER = 2;
    const float SECONDS_PER_MINUTE = 60;


    string[] chartAsLines;
    public ChartParser(string filePath)
    {
        chartAsLines = File.ReadAllLines(filePath);
        ParseChartData();
    }

    // Make accessing this more efficient
    // Possibly in another object or collection where you access dictionaries based on type requested?
    public SortedDictionary<int, BPMData> bpmEvents;
    public SortedDictionary<int, TSData> tsEvents;
    public int resolution;
    public Metadata metadata;

    void ParseChartData()
    {
        var eventGroups = FormatEventSections();

        foreach (var eventGroup in eventGroups)
        {
            switch (eventGroup.EventGroupIdentifier)
            {
                case ChartEventGroup.HeaderType.Song: // required (needs exception handling)
                    (metadata, resolution) = ParseSongMetadata(eventGroup);
                    break;
                case ChartEventGroup.HeaderType.SyncTrack: // required (needs exception handling)
                    (bpmEvents, tsEvents) = ParseSyncTrack(eventGroup);
                    break;
                case ChartEventGroup.HeaderType.Events:
                    // events parse
                    break;
                default:
                    // generic instrument parse
                    break;
            }
        }

        // also need to parse chart stems
        // find properly named files - add to stems
        // find other audio files - ask to assign
    }

    (Metadata, int) ParseSongMetadata(ChartEventGroup songEventGroup)
    {
        Metadata metadata = new();
        if (File.Exists($"{Chart.FolderPath}/song.ini")) // read from ini if exists (most reliable scenario)
        {
            var iniEventGroup = InitializeEventGroup($"{Chart.FolderPath}/song.ini");

            foreach (var kvp in iniEventGroup.data)
            {
                if (Enum.TryParse(typeof(Metadata.MetadataType), kvp.Key, true, out var formattedKey))
                {
                    metadata.SongInfo.Add((Metadata.MetadataType)formattedKey, kvp.Value);
                }
                else if (Enum.TryParse(typeof(Metadata.InstrumentDifficultyType), kvp.Key, true, out var formattedInstrumentDiff))
                {
                    if (int.TryParse(kvp.Value, out int instrumentDifficulty))
                    {
                        metadata.Difficulties.Add((Metadata.InstrumentDifficultyType)formattedInstrumentDiff, instrumentDifficulty);
                    }
                    // log warning about unrecognized key
                }
            }
        }
        else // read what we can from embedded .chart data
        {
            // log warning about ini being more efficient
            foreach (var kvp in songEventGroup.data)
            {
                if (Enum.TryParse(typeof(Metadata.MetadataType), kvp.Key.Trim(), true, out var iniFormattedKey))
                {
                    var formattedValue = kvp.Value.Replace("\"", "").Replace(", ", "");
                    metadata.SongInfo.Add((Metadata.MetadataType)iniFormattedKey, formattedValue);
                }
            }
        }
        var resolutionData = songEventGroup.data.Where(list => list.Key.Trim() == "Resolution").ToList();

        if (!int.TryParse(resolutionData[0].Value, out int resolutionValue))
            throw new ArgumentException($"Resolution data is not valid. {HELPFUL_REMINDER}");

        return (metadata, resolutionValue);
    }

    (SortedDictionary<int, BPMData>, SortedDictionary<int, TSData>) ParseSyncTrack(ChartEventGroup syncTrackEventGroup)
    {
        var events = syncTrackEventGroup.data;

        List<int> tempoTickTimeKeys = new();
        List<float> bpmVals = new();
        SortedDictionary<int, TSData> tsEvents = new();

        foreach (var entry in events)
        {
            if (!entry.Value.Contains(TEMPO_EVENT_INDICATOR) && !entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
                throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid event identifier. {HELPFUL_REMINDER}");

            if (!int.TryParse(entry.Key, out int tickValue))
                throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid Key. {HELPFUL_REMINDER}");

            if (entry.Value.Contains(TEMPO_EVENT_INDICATOR))
            {
                tempoTickTimeKeys.Add(tickValue);

                var eventData = entry.Value;
                eventData = eventData.Replace($"{TEMPO_EVENT_INDICATOR} ", ""); // SPACE IS VERY IMPORTANT HERE

                if (!int.TryParse(eventData, out int bpmNoDecimal))
                    throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid tempo entry. {HELPFUL_REMINDER}");

                double bpmWithDecimal = bpmNoDecimal / BPM_FORMAT_CONVERSION;
                bpmVals.Add((float)Math.Round(bpmWithDecimal, 3));
            }
            else if (entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
            {
                var eventData = entry.Value;
                eventData = eventData.Replace($"{TIME_SIGNATURE_EVENT_INDICATOR} ", "");

                string[] tsParts = eventData.Split(" ");

                if (!int.TryParse(tsParts[0], out int numerator))
                    throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature numerator. {HELPFUL_REMINDER}");

                int denominator = DEFAULT_TS_DENOMINATOR;
                if (tsParts.Length == 2) // There is no space in the event value (only one number)
                {
                    if (int.TryParse(tsParts[1], out int denominatorLog2))
                        throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature denominator. {HELPFUL_REMINDER}");
                    denominator = (int)Math.Pow(TS_POWER_CONVERSION_NUMBER, denominatorLog2);
                }

                tsEvents.Add(tickValue, new TSData(numerator, denominator));
            }
        }

        var bpmEvents = FormatBPMDictionary(tempoTickTimeKeys, bpmVals);

        return (bpmEvents, tsEvents);
    }

    SortedDictionary<int, BPMData> FormatBPMDictionary(List<int> ticks, List<float> bpms)
    {
        SortedDictionary<int, BPMData> outputDict = new();

        double currentSongTime = 0;
        for (int i = 0; i < ticks.Count; i++)
        {
            if (i > 0)
            {
                var tickDelta = ticks[i] - ticks[i - 1];
                double timeDelta = tickDelta / (double)resolution * SECONDS_PER_MINUTE / bpms[i - 1];
                currentSongTime += timeDelta;
            }

            outputDict.Add(ticks[i], new BPMData(bpms[i], (float)currentSongTime));
        }
        return outputDict;
    }

    List<ChartEventGroup> FormatEventSections()
    {
        List<ChartEventGroup> identifiedSections = new();
        for (int lineNumber = 0; lineNumber < chartAsLines.Length - 1; lineNumber++)
        {
            if (chartAsLines[lineNumber].Contains("["))
                identifiedSections.Add(InitializeEventGroup(lineNumber));
        }
        return identifiedSections;
    }

    ChartEventGroup InitializeEventGroup(int lineIndex)
    {
        var identifierLine = chartAsLines[lineIndex];
        var cleanIdentifier = identifierLine.Replace("[", "").Replace("]", "");

        ChartEventGroup identifiedSection = new(
            (ChartEventGroup.HeaderType)Enum.Parse(typeof(ChartEventGroup.HeaderType), cleanIdentifier, true)
            );

        if (chartAsLines[lineIndex + 1] != "{") // line with { to mark beginning of section
            throw new ArgumentException($"{identifiedSection.EventGroupIdentifier} is not enclosed properly. {HELPFUL_REMINDER}");

        lineIndex += 2; // line with first bit of data
        List<string> eventData = new();
        string workingLine = chartAsLines[lineIndex];

        // this needs more exception handling (not properly enclosed sections, etc.)
        while (workingLine != "}" && lineIndex < chartAsLines.Length - 1)
        {
            eventData.Add(workingLine);

            lineIndex++;
            workingLine = chartAsLines[lineIndex];
        }

        var kvpConversion = eventData.Select(line =>
        {
            var parts = line.Split(" = ", 2);
            return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
        }).ToList();


        identifiedSection.data = kvpConversion;
        return identifiedSection;
    }

    ChartEventGroup InitializeEventGroup(string iniPath)
    {
        var iniData = File.ReadAllLines(iniPath);
        ChartEventGroup iniGroup = new(ChartEventGroup.HeaderType.Song);

        List<KeyValuePair<string, string>> eventData = new();

        for (int lineIndex = 1; lineIndex < iniData.Length; lineIndex++)
        {
            var lineParts = iniData[lineIndex].Split(" = ");
            if (lineParts.Length > 1)
                eventData.Add(new KeyValuePair<string, string>(lineParts[0].Trim(), lineParts[1].Trim()));
        }

        iniGroup.data = eventData;
        return iniGroup;
    }
    
    

}

class ChartEventGroup
{
    public enum HeaderType
    {
        Song,
        SyncTrack,
        Events,
        EasySingle,
        MediumSingle,
        HardSingle,
        ExpertSingle,
        EasyDoubleGuitar,
        MediumDoubleGuitar,
        HardDoubleGuitar,
        ExpertDoubleGuitar,
        EasyDoubleBass,
        MediumDoubleBass,
        HardDoubleBass,
        ExpertDoubleBass,
        EasyDoubleRhythm,
        MediumDoubleRhythm,
        HardDoubleRhythm,
        ExpertDoubleRhythm,
        EasyDrums,
        MediumDrums,
        HardDrums,
        ExpertDrums,
        EasyKeyboard,
        MediumKeyboard,
        HardKeyboard,
        ExpertKeyboard,
        EasyGHLGuitar,
        MediumGHLGuitar,
        HardGHLGuitar,
        ExpertGHLGuitar,
        EasyGHLBass,
        MediumGHLBass,
        HardGHLBass,
        ExpertGHLBass,
        EasyGHLCoop,
        MediumGHLCoop,
        HardGHLCoop,
        ExpertGHLCoop,
        EasyGHLRhythm,
        MediumGHLRhythm,
        HardGHLRhythm,
        ExpertGHLRhythm,
        EasyVox,
        MediumVox,
        HardVox,
        ExpertVox
    }

    public HeaderType EventGroupIdentifier;
    public List<KeyValuePair<string, string>> data;

    public ChartEventGroup(HeaderType identifier)
    {
        EventGroupIdentifier = identifier;
    }
} 
