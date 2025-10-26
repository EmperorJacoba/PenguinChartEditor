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
    const string FORCED_SUBSTRING = "N 5 0";
    const string TAP_SUBSTRING = "N 6 0";
    const int EVENT_DATA_INDEX = 1;
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

        Parallel.ForEach(eventGroups, item => ProcessEventGroup(item));

        // also need to parse chart stems
        // find properly named files - add to stems
        // find other audio files - ask to assign
    }

    void ProcessEventGroup(ChartEventGroup eventGroup)
    {
        if (eventGroup == null) return;
        switch (eventGroup.EventGroupIdentifier)
        {
            case HeaderType.SyncTrack: // required (needs exception handling)
                (bpmEvents, tsEvents) = ParseSyncTrack(eventGroup);
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

        List<ChartEventGroup> identifiedSections = new();

        // song data is processed seperately (string, string) vs (int, string) with all other sections
        // get it done first to get resolution handled => essential for sync track and others
        if (sectionHeaderCandidates.Contains(SONG_HEADER_LOCATION))
        {
            sectionHeaderCandidates.Remove(SONG_HEADER_LOCATION);

            var songData = InitializeSongGroup(SONG_HEADER_LOCATION);
            (metadata, resolution) = ParseSongMetadata(songData);
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
    (Metadata, int) ParseSongMetadata(SongDataGroup songEventGroup)
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
        HashSet<int> anchoredTicks = new();

        foreach (var entry in events)
        {
            if (!entry.Value.Contains(TEMPO_EVENT_INDICATOR) && !entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR) && !entry.Value.Contains(ANCHOR_INDICATOR))
                continue;

            if (entry.Value.Contains(TEMPO_EVENT_INDICATOR))
            {
                tempoTickTimeKeys.Add(entry.Key);

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
                    if (!int.TryParse(tsParts[1], out int denominatorLog2))
                        throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature denominator. {HELPFUL_REMINDER}");
                    denominator = (int)Math.Pow(TS_POWER_CONVERSION_NUMBER, denominatorLog2);
                }

                tsEvents.Add(entry.Key, new TSData(numerator, denominator));
            }
            else if (entry.Value.Contains(ANCHOR_INDICATOR))
            {
                anchoredTicks.Add(entry.Key);

                // if for some reason you need to add parsing for the microsecond value do it here
                // that is not here because a) penguin already works with and calculates the timestamps of every event
                // and b) if the microsecond value is parsed and it's not aligned with the Format calculations,
                // then what is penguin supposed to do? change the incoming BPM data? no
                // I think it has the microsecond value for programs that choose not to work with timestamps
                // (timestamps are easier to deal with in my opinion, even if an extra (minor) step is needed after every edit)
            }
        }

        var bpmEvents = FormatBPMDictionary(tempoTickTimeKeys, bpmVals, anchoredTicks);

        return (bpmEvents, tsEvents);
    }

    SortedDictionary<int, BPMData> FormatBPMDictionary(List<int> ticks, List<float> bpms, HashSet<int> anchors)
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

            outputDict.Add
                (ticks[i], 
                new BPMData(
                    bpms[i], 
                    (float)currentSongTime,
                    anchors.Contains(ticks[i]))
                );
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
        // this is where all parsed data ends up, data is processed lane-by-lane
        SortedDictionary<int, FiveFretNoteData>[] lanes = new SortedDictionary<int, FiveFretNoteData>[6] { new(), new(), new(), new(), new(), new() };

        // this is for simplifying hopo checks - initialized with -hopoCutoff to prevent the first note from starting as a hopo
        int[] lastNoteTicks = new int[6] { -hopoCutoff, -hopoCutoff, -hopoCutoff, -hopoCutoff, -hopoCutoff, -hopoCutoff };

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

        HashSet<int> uniqueTicks = chartEventGroup.data.Select(item => item.Key).ToHashSet();

        foreach (var uniqueTick in uniqueTicks)
        {
            var eventsAtTick = chartEventGroup.data.Where(item => item.Key == uniqueTick).Select(item => item.Value).ToList();
            bool tapModifier = false;
            bool forcedModifier = false;

            if (eventsAtTick.Contains($"{FORCED_SUBSTRING}"))
            {
                forcedModifier = true;
                eventsAtTick.Remove($"{FORCED_SUBSTRING}");
            }

            if (eventsAtTick.Contains($"{TAP_SUBSTRING}"))
            {
                tapModifier = true;
                eventsAtTick.Remove($"{TAP_SUBSTRING}");
            }

            var noteCount = 0;
            for (int i = 0; i < eventsAtTick.Count; i++)
            {
                if (eventsAtTick[i].Contains("N")) noteCount++;
            }

            int noteIdentifier;
            int sustain;
            foreach (var @event in eventsAtTick)
            {
                var values = @event.Split(' ');
                switch (values[IDENTIFIER_INDEX])
                {
                    case NOTE_INDICATOR:

                        if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
                            throw new ArgumentException($"Invalid note identifier for {instrument} @ tick {uniqueTick}: {values[NOTE_IDENTIFIER_INDEX]}");

                        if (noteIdentifier > LAST_VALID_IDENTIFIER) continue;

                        if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                            throw new ArgumentException($"Invalid sustain for {instrument} @ tick {uniqueTick}: {values[SUSTAIN_INDEX]}");

                        FiveFretInstrument.LaneOrientation lane;
                        if (noteIdentifier != OPEN_IDENTIFIER)
                            lane = (FiveFretInstrument.LaneOrientation)noteIdentifier;
                        else lane = FiveFretInstrument.LaneOrientation.open; // open identifier is not the same as lane orientation (index of lane dictionary)

                        bool defaultOrientation = true; // equivilent to forced
                        FiveFretNoteData.FlagType flagType;
                        if (tapModifier)
                        {
                            flagType = FiveFretNoteData.FlagType.tap; // tap overrides any hopo/forcing logic
                        }
                        else
                        {
                            bool hopoEligible = false;

                            for (var j = 0; j < lastNoteTicks.Length; j++)
                            {
                                if ((FiveFretInstrument.LaneOrientation)j == lane || lastNoteTicks[j] == uniqueTick) continue;

                                if (uniqueTick - lastNoteTicks[j] < hopoCutoff && noteCount == 1)
                                {
                                    hopoEligible = true;
                                    break;
                                }
                            }

                            if (forcedModifier)
                            {
                                hopoEligible = !hopoEligible;
                                defaultOrientation = false;
                            }

                            flagType = hopoEligible ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;
                        }

                        var noteData = new FiveFretNoteData(sustain, flagType, defaultOrientation);

                        lanes[(int)lane].Add(uniqueTick, noteData);
                        lastNoteTicks[(int)lane] = uniqueTick;

                        break;
                    case SPECIAL_INDICATOR:

                        if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
                            throw new ArgumentException($"Invalid special identifier for {instrument} @ tick {uniqueTick}: {values[NOTE_IDENTIFIER_INDEX]}");

                        if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                            throw new ArgumentException($"Invalid sustain for {instrument} @ tick {uniqueTick}: {values[SUSTAIN_INDEX]}");

                        if (noteIdentifier != STARPOWER_INDICATOR) continue; // should only have starpower indicator, no fills or anything

                        starpower.Add(uniqueTick, new SpecialData(sustain, SpecialData.EventType.starpower));

                        break;
                    case EVENT_INDICATOR:
                        if (!Enum.TryParse(typeof(LocalEventData.EventType), values[EVENT_DATA_INDEX], true, out var localEvent))
                            throw new ArgumentException($"Error at {uniqueTick}: Unsupported event type: {values[EVENT_DATA_INDEX]}");

                        localEvents.Add(uniqueTick, new LocalEventData((LocalEventData.EventType)localEvent));

                        break;
                    case DEPRECATED_HAND_INDICATOR:
                        continue;
                }
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