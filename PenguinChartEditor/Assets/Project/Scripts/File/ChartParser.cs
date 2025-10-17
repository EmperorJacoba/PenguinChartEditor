using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SFB;

public class ChartParser
{
    #region Metadata Constants

    const string QUOTES_STRING = "\"";
    const string YEAR_COMMA = ", ";

    #endregion

    #region Sync Track Constants
    const string HELPFUL_REMINDER = "Please check the file and try again.";

    // Note: When creating new chart file, make sure [SyncTrack] starts with a BPM and TS declaration!
    const int DEFAULT_TS_DENOMINATOR = 4;
    const string TEMPO_EVENT_INDICATOR = "B";
    const string TIME_SIGNATURE_EVENT_INDICATOR = "TS";
    const string SYNC_TRACK_ERROR = "[SyncTrack] has invalid tempo event:";
    const float BPM_FORMAT_CONVERSION = 1000.0f;
    const int TS_POWER_CONVERSION_NUMBER = 2;
    const float SECONDS_PER_MINUTE = 60;

    #endregion

    #region Five Fret Instrument Constants

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
    const string FORCED_SUBSTRING = "N 5";
    const string TAP_SUBSTRING = "N 6";
    const int EVENT_DATA_INDEX = 1;
    const int FORCED_IDENTIFIER = 5;
    const int TAP_IDENTIFIER = 6;
    const int LAST_VALID_IDENTIFIER = 7;
    const int OPEN_IDENTIFIER = 7;
    const int STARPOWER_INDICATOR = 2;

    #endregion

    #region Accessors 
    // Make accessing this more efficient
    // Possibly in another object or collection where you access dictionaries based on type requested?
    public SortedDictionary<int, BPMData> bpmEvents;
    public SortedDictionary<int, TSData> tsEvents;
    public int resolution;
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

    #endregion

    #region Event Section Setup

    List<ChartEventGroup> FormatEventSections()
    {
        List<ChartEventGroup> identifiedSections = new();
        for (int lineNumber = 0; lineNumber < chartAsLines.Length; lineNumber++)
        {
            if (chartAsLines[lineNumber].Contains("["))
                identifiedSections.Add(InitializeEventGroup(lineNumber));
        }
        return identifiedSections;
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

    /// <summary>
    /// Make a new ChartEventGroup from an ini file containing song metadata. 
    /// </summary>
    /// <param name="iniPath">File path of the .ini file.</param>
    /// <returns>ChartEventGroup with data from .ini file.</returns>
    ChartEventGroup InitializeEventGroup(string iniPath)
    {
        var iniData = File.ReadAllLines(iniPath);
        ChartEventGroup iniGroup = new(ChartEventGroup.HeaderType.Song);

        List<KeyValuePair<string, string>> eventData = new();

        // index 0 is [Song] - not necessary
        for (int lineIndex = 1; lineIndex < iniData.Length; lineIndex++)
        {
            var lineParts = iniData[lineIndex].Split(" = ");
            if (lineParts.Length > 1)
                eventData.Add(new KeyValuePair<string, string>(lineParts[0].Trim(), lineParts[1].Trim()));
        }

        iniGroup.data = eventData;
        return iniGroup;
    }

    #endregion

    #region Metadata

    /// <summary>
    /// 
    /// </summary>
    /// <param name="songEventGroup">ChartEventGroup object with [Song] header.</param>
    /// <returns>Constructed Metadata, resolution</returns>
    /// <exception cref="ArgumentException"></exception>
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
        var resolutionData = songEventGroup.data.Where(list => list.Key == "Resolution").ToList();

        if (!int.TryParse(resolutionData[0].Value, out int resolutionValue))
            throw new ArgumentException($"Resolution data is not valid. {HELPFUL_REMINDER}");

        return (metadata, resolutionValue);
    }

    #endregion

    #region SyncTrack

    /// <summary>
    /// 
    /// </summary>
    /// <param name="syncTrackEventGroup"></param>
    /// <returns>BPM event data, TS event data</returns>
    /// <exception cref="ArgumentException"></exception>
    (SortedDictionary<int, BPMData>, SortedDictionary<int, TSData>) ParseSyncTrack(ChartEventGroup syncTrackEventGroup)
    {
        var events = syncTrackEventGroup.data;

        List<int> tempoTickTimeKeys = new();
        List<float> bpmVals = new();
        SortedDictionary<int, TSData> tsEvents = new();

        foreach (var entry in events)
        {
            if (!entry.Value.Contains(TEMPO_EVENT_INDICATOR) && !entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
                continue;

            if (!int.TryParse(entry.Key, out int tickValue))
                throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid Key. {HELPFUL_REMINDER}");

            if (entry.Value.Contains(TEMPO_EVENT_INDICATOR))
            {
                tempoTickTimeKeys.Add(tickValue);

                var eventData = entry.Value;
                eventData = eventData.Replace($"{TEMPO_EVENT_INDICATOR} ", ""); // SPACE IS VERY IMPORTANT HERE

                if (!int.TryParse(eventData, out int bpmNoDecimal))
                    throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid tempo entry. {HELPFUL_REMINDER}");

                float bpmWithDecimal = bpmNoDecimal / BPM_FORMAT_CONVERSION;
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
                // from Chart File Format Specifications
                var tickDelta = ticks[i] - ticks[i - 1];
                double timeDelta = tickDelta / (double)resolution * SECONDS_PER_MINUTE / bpms[i - 1];
                currentSongTime += timeDelta;
            }

            outputDict.Add(ticks[i], new BPMData(bpms[i], (float)currentSongTime));
        }
        return outputDict;
    }

    #endregion

    #region Instruments
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
        //Chart.Log("Skipped instrument group");
        return null;
        // throw new ArgumentException("Tried to parse an unsupported instrument group.");
    }

    FiveFretInstrument ParseFiveFret(ChartEventGroup chartEventGroup)
    {
        SortedDictionary<int, FiveFretNoteData>[] lanes = new SortedDictionary<int, FiveFretNoteData>[6] { new(), new(), new(), new(), new(), new() };
        SortedDictionary<int, SpecialData> starpower = new();
        SortedDictionary<int, LocalEventData> localEvents = new();

        InstrumentType instrument = (int)chartEventGroup.EventGroupIdentifier switch
        {
            <= 13 => InstrumentType.guitar,
            <= 23 => InstrumentType.coopGuitar,
            <= 33 => InstrumentType.bass,
            <= 43 => InstrumentType.rhythm,
            <= 53 => InstrumentType.keys,
            _ => throw new ArgumentException("Tried to identify an invalid instrument.")
        };

        DifficultyType difficulty = chartEventGroup.GetDifficulty();

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
                        throw new ArgumentException($"Invalid note identifier for {instrument} @ tick {tickValue}: {values[NOTE_IDENTIFIER_INDEX]}");

                    if (noteIdentifier == FORCED_IDENTIFIER || noteIdentifier == TAP_IDENTIFIER || noteIdentifier > LAST_VALID_IDENTIFIER) continue;

                    if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                        throw new ArgumentException($"Invalid sustain for {instrument} @ tick {tickValue}: {values[SUSTAIN_INDEX]}");

                    FiveFretInstrument.LaneOrientation lane;
                    if (noteIdentifier != OPEN_IDENTIFIER)
                        lane = (FiveFretInstrument.LaneOrientation)noteIdentifier;
                    else lane = FiveFretInstrument.LaneOrientation.open; // open identifier is not the same as lane orientation (index of lane dictionary)

                    // looks wack, but this just whittles down the main KVP list into just forced & tap identifiers for this tick
                    var modifierEventsAtNoteIndex = chartEventGroup.data.Where(
                        eventPair => eventPair.Key == chartEventGroup.data[i].Key // get only events at this tick
                        && (eventPair.Value.Contains(FORCED_SUBSTRING) || eventPair.Value.Contains(TAP_SUBSTRING)) // filter for taps and hopos
                        ).Select(kvp =>
                        {
                            int lastSpace = kvp.Value.LastIndexOf(" ");
                            return lastSpace >= 0 ? kvp.Value[..lastSpace] : kvp.Value;
                        }).ToList(); // get only the values sans 0 sustain, keys are irrelevant 

                    // calculate if note is strum or tap or hopo

                    FiveFretNoteData.FlagType flagType;
                    if (modifierEventsAtNoteIndex.Contains(TAP_SUBSTRING))
                    {
                        flagType = FiveFretNoteData.FlagType.tap; // tap overrides any hopo/forcing logic
                    }
                    else
                    {
                        bool hopoEligible = false;
                        for (var j = 0; j < lanes.Length - 1; j++)
                        {
                            if ((FiveFretInstrument.LaneOrientation)j == lane) continue;

                            var closeEvents = lanes[j].Where
                            (kvp => tickValue - kvp.Key < hopoCutoff && kvp.Key != tickValue) // hopo eligibility does not apply to chord note groups (e.g a green and red note on the same tick)
                            .ToList();

                            if (closeEvents.Count() > 0)
                            {
                                hopoEligible = true;
                                break;
                            }
                        }
                        if (modifierEventsAtNoteIndex.Contains(FORCED_SUBSTRING))
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
                        throw new ArgumentException($"Invalid special identifier for {instrument} @ tick {tickValue}: {values[NOTE_IDENTIFIER_INDEX]}");

                    if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                        throw new ArgumentException($"Invalid sustain for {instrument} @ tick {tickValue}: {values[SUSTAIN_INDEX]}");

                    if (noteIdentifier != STARPOWER_INDICATOR) continue; // should only have starpower indicator, no fills or anything

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

        return new FiveFretInstrument(lanes, starpower, localEvents, instrument, difficulty);
    }

    #endregion
}

#region ChartEventGroup construction

/// <summary>
/// Object that contains event section data (enclosed in [SectionName] { ... }) in deconstructed KeyValuePairs. 
/// </summary>
class ChartEventGroup
{
    /// <summary>
    /// Contains possible section headers enclosed as [Name] in a .chart file.
    /// Identifiers follow a pattern based on instrument parsing needs. Metadata/tempo has values 10^0, Five-fret is 10^1, Drums is 10^2, GHL is 10^3, Vox is 10^4.
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

    public DifficultyType GetDifficulty()
    {
        return ((int)EventGroupIdentifier % 10) switch
        {
            0 => DifficultyType.easy,
            1 => DifficultyType.medium,
            2 => DifficultyType.hard,
            3 => DifficultyType.expert,
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

#endregion