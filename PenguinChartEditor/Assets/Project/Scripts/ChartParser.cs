using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEngine.SocialPlatforms;

public class ChartParser
{
    //////////////////////////////
    //  SYNC TRACK CONSTANTS    //
    //////////////////////////////

    const string HELPFUL_REMINDER = "Please check the file and try again.";

    // Note: When creating new chart file, make sure [SyncTrack] starts with a BPM and TS declaration!
    const int DEFAULT_TS_DENOMINATOR = 4;
    const string TEMPO_EVENT_INDICATOR = "B";
    const string TIME_SIGNATURE_EVENT_INDICATOR = "TS";
    const string SYNC_TRACK_ERROR = "[SyncTrack] has invalid tempo event:";
    const float BPM_FORMAT_CONVERSION = 1000.0f;
    const int TS_POWER_CONVERSION_NUMBER = 2;
    const float SECONDS_PER_MINUTE = 60;

    //////////////////////////
    // INSTRUMENT CONSTANTS //
    //////////////////////////

    int hopoCutoff
    {
        get
        {
            return (int)Math.Floor(((float)65 / 192) * (float)resolution);
        }
    }
    const string NOTE_INDICATOR = "N";
    const string SPECIAL_INDICATOR = "S";
    const string EVENT_INDICATOR = "E";
    const string DEPRECATED_HAND_INDICATOR = "H";
    const int IDENTIFIER_INDEX = 0;
    const int NOTE_IDENTIFIER_INDEX = 1;
    const int SUSTAIN_INDEX = 2;
    const string FORCED_IDENTIFIER = "N 5";
    const string TAP_IDENTIFIER = "N 6";
    const int EVENT_DATA_INDEX = 1;

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
    public List<IInstrument> instruments = new();

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
                    instruments.Add(ParseInstrumentGroup(eventGroup));
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
                if (Enum.TryParse(typeof(Metadata.MetadataType), kvp.Key, true, out var iniFormattedKey))
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

    IInstrument ParseInstrumentGroup(ChartEventGroup chartEventGroup)
    {
        switch (chartEventGroup.GetInstrumentGroup())
        {
            case ChartEventGroup.InstrumentGroup.FiveFret:
                return ParseFiveFret(chartEventGroup);
            case ChartEventGroup.InstrumentGroup.FourLaneDrums:
                // parse drums
                break;
            case ChartEventGroup.InstrumentGroup.GHL:
                // parse GHL
                break;
            case ChartEventGroup.InstrumentGroup.Vox:
                // parse vox
                break;
        }
        throw new ArgumentException("Tried to parse an unsupported instrument group.");
    }

    FiveFretInstrument ParseFiveFret(ChartEventGroup chartEventGroup)
    {
        SortedDictionary<int, FiveFretNoteData>[] lanes = new SortedDictionary<int, FiveFretNoteData>[6] { new(), new(), new(), new(), new(), new() };
        SortedDictionary<int, SpecialData> starpower = new();
        SortedDictionary<int, LocalEventData> localEvents = new();

        for (int i = 0; i < chartEventGroup.data.Count; i++)
        {
            var entry = chartEventGroup.data[i];
            if (!int.TryParse(entry.Key, out int tickValue))
                throw new ArgumentException($"[{entry.Key} = {entry.Value}]. Error type: Invalid Key. {HELPFUL_REMINDER}");

            // 0 will be identifier
            // 1 will be type for non-events
            // 2 will be sustain for non-events
            // for events 0 will be E and 1 will be data
            var values = entry.Value.Split(' ');

            int noteIdentifier; 
            int sustain;
            switch (values[IDENTIFIER_INDEX])
            {
                case NOTE_INDICATOR:

                    if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
                        throw new ArgumentException();

                    if (noteIdentifier == 5 || noteIdentifier == 6 || noteIdentifier >= 8) continue;

                    if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                        throw new ArgumentException();

                    FiveFretInstrument.LaneOrientation lane;
                    if (noteIdentifier != 7)
                        lane = (FiveFretInstrument.LaneOrientation)noteIdentifier;
                    else lane = FiveFretInstrument.LaneOrientation.open;

                    var modifierEventsAtNoteIndex = chartEventGroup.data.Where(
                        eventPair => eventPair.Key == chartEventGroup.data[i].Key // get only events at this tick
                        && (eventPair.Value.Contains(FORCED_IDENTIFIER) || eventPair.Value.Contains(TAP_IDENTIFIER)) // filter for taps and hopos
                        ).Select(kvp =>
                        {
                            int lastSpace = kvp.Value.LastIndexOf(" ");
                            return lastSpace >= 0 ? kvp.Value[..lastSpace] : kvp.Value;
                        }).ToList(); // get only the values sans 0 sustain, keys are irrelevant 

                    // calculate if note is strum or tap or hopo

                    FiveFretNoteData.FlagType flagType;
                    if (modifierEventsAtNoteIndex.Contains(TAP_IDENTIFIER))
                    {
                        flagType = FiveFretNoteData.FlagType.tap;
                    }
                    else
                    {
                        bool hopoEligible = false;
                        for (var j = 0; j < lanes.Length; j++)
                        {
                            if ((FiveFretInstrument.LaneOrientation)j == lane) continue;

                            var closeEvents = lanes[i].Where(kvp => tickValue - kvp.Key < hopoCutoff);
                            if (closeEvents.Count() > 0)
                            {
                                hopoEligible = true;
                                break;
                            }
                        }
                        if (modifierEventsAtNoteIndex.Contains(FORCED_IDENTIFIER))
                        {
                            hopoEligible = !hopoEligible;
                        }
                        flagType = hopoEligible ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;
                    }

                    var noteData = new FiveFretNoteData(sustain, flagType);
                    lanes[noteIdentifier].Add(tickValue, noteData);

                    break;
                case SPECIAL_INDICATOR:

                    if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
                        throw new ArgumentException();

                    if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                        throw new ArgumentException();

                    if (noteIdentifier != 2) continue;

                    starpower.Add(tickValue, new SpecialData(sustain, SpecialData.EventType.starpower));

                    break;
                case EVENT_INDICATOR:
                    if (!Enum.TryParse(typeof(LocalEventData.EventType), values[EVENT_DATA_INDEX], true, out var localEvent))
                        throw new ArgumentException($"Error at {tickValue}: Unsupported event type: {values[EVENT_DATA_INDEX]}");

                    localEvents.Add(tickValue, new LocalEventData((LocalEventData.EventType)localEvent));

                    break;
                case DEPRECATED_HAND_INDICATOR:
                    continue;
            }
        }

        return new FiveFretInstrument(lanes, starpower, localEvents);
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
    /// <summary>
    /// Contains possible section headers enclosed as [Name] in a .chart file.
    /// Identifiers follow a pattern based on instrument parsing needs. Metadata/tempo has values 10^1, Five-fret is 10^2, GHL is 10^3, Vox is 10^4.
    /// Difficulties: E = 0, M = 1, H = 2, X = 3
    /// <para> Example: Song = 0, EasySingle (Easy Guitar) = 10, MediumDrums = 101 </para>
    /// </summary>
    public enum HeaderType
    {
        Song = 0,
        SyncTrack = 1,
        Events = 2,

        EasySingle = 10,
        MediumSingle = 11,
        HardSingle = 12,
        ExpertSingle = 13,

        EasyDoubleGuitar = 20,
        MediumDoubleGuitar = 21,
        HardDoubleGuitar = 22,
        ExpertDoubleGuitar = 23,

        EasyDoubleBass = 30,
        MediumDoubleBass = 31,
        HardDoubleBass = 32,
        ExpertDoubleBass = 33,

        EasyDoubleRhythm = 40,
        MediumDoubleRhythm = 41,
        HardDoubleRhythm = 42,
        ExpertDoubleRhythm = 43,

        EasyKeyboard = 50,
        MediumKeyboard = 51,
        HardKeyboard = 52,
        ExpertKeyboard = 53,

        EasyDrums = 100,
        MediumDrums = 101,
        HardDrums = 102,
        ExpertDrums = 103,

        EasyGHLGuitar = 1000,
        MediumGHLGuitar = 1001,
        HardGHLGuitar = 1002,
        ExpertGHLGuitar = 1003,

        EasyGHLBass = 1010,
        MediumGHLBass = 1011,
        HardGHLBass = 1012,
        ExpertGHLBass = 1013,

        EasyGHLCoop = 1020,
        MediumGHLCoop = 1021,
        HardGHLCoop = 1022,
        ExpertGHLCoop = 1023,

        EasyGHLRhythm = 1030,
        MediumGHLRhythm = 1031,
        HardGHLRhythm = 1032,
        ExpertGHLRhythm = 1033,

        EasyVox = 10000,
        MediumVox = 10001,
        HardVox = 10002,
        ExpertVox = 10003
    }

    public enum InstrumentGroup
    {
        None,
        FiveFret,
        FourLaneDrums,
        GHL,
        Vox,
    }

    public enum Difficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Expert = 3
    }

    public InstrumentGroup GetInstrumentGroup()
    {
        return (int)EventGroupIdentifier switch
        {
            < 10 => InstrumentGroup.None,
            < 100 => InstrumentGroup.FiveFret,
            < 1000 => InstrumentGroup.FourLaneDrums,
            < 10000 => InstrumentGroup.GHL,
            < 100000 => InstrumentGroup.Vox,
            _ => throw new ArgumentException("Tried to get invalid instrument group.")
        };
    }

    public Difficulty GetDifficulty()
    {
        return ((int)EventGroupIdentifier % 10) switch
        {
            0 => Difficulty.Easy,
            1 => Difficulty.Medium,
            2 => Difficulty.Hard,
            3 => Difficulty.Expert,
            _ => throw new ArgumentException("Tried to get invalid instrument difficulty.")
        };
    }

    public HeaderType EventGroupIdentifier;
    public List<KeyValuePair<string, string>> data;

    public ChartEventGroup(HeaderType identifier)
    {
        EventGroupIdentifier = identifier;
    }
    
} 
