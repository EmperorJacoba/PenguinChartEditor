using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Dynamic;
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

    void ParseChartData()
    {
        var eventGroups = FormatEventSections();

        foreach (var eventGroup in eventGroups)
        {
            switch (eventGroup.EventGroupIdentifier)
            {
                case ChartEventGroup.HeaderType.Song: // required (needs exception handling)
                    // metadata parse
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
    }

    public static int loadedChartResolution = UserSettings.DefaultResolution;

    (SortedDictionary<int, BPMData>, SortedDictionary<int, TSData>) ParseSyncTrack(ChartEventGroup syncTrackEventGroup)
    {
        loadedChartResolution = GetResolution(chartAsLines);

        var events = syncTrackEventGroup.data;

        List<int> tempoTickTimeKeys = new();
        List<float> bpmVals = new();
        SortedDictionary<int, TSData> tsEvents = new();

        foreach (var entry in events)
        {
            if (!entry.Value.Contains(TEMPO_EVENT_INDICATOR) || !entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
                throw new ArgumentException($"{SYNC_TRACK_ERROR} [SyncTrack] has invalid tempo event: [{entry.Key} = {entry.Value}]. Error type: Invalid event identifier. {HELPFUL_REMINDER}");

            if (!int.TryParse(entry.Key, out int tickValue))
                throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid Key. {HELPFUL_REMINDER}");

            if (entry.Value.Contains(TEMPO_EVENT_INDICATOR))
            {
                tempoTickTimeKeys.Add(tickValue);

                var eventData = entry.Value;
                eventData.Replace($"{TEMPO_EVENT_INDICATOR} ", ""); // SPACE IS VERY IMPORTANT HERE

                if (!int.TryParse(eventData, out int bpmNoDecimal))
                    throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid tempo entry. {HELPFUL_REMINDER}");

                double bpmWithDecimal = bpmNoDecimal / BPM_FORMAT_CONVERSION;
                bpmVals.Add((float)Math.Round(bpmWithDecimal, 3));
            }
            else if (entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
            {
                var eventData = entry.Value;
                eventData.Replace($"{TIME_SIGNATURE_EVENT_INDICATOR} ", "");

                string[] tsParts = eventData.Split(" ");

                if (!int.TryParse(tsParts[0], out int numerator))
                    throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature numerator. {HELPFUL_REMINDER}");

                int denominator = DEFAULT_TS_DENOMINATOR;
                if (tsParts.Length == 2) // There is no space in the event value (only one number)
                {
                    if (int.TryParse(tsParts[1], out int denominatorBase2))
                        throw new ArgumentException($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature denominator. {HELPFUL_REMINDER}");
                    denominator = (int)Math.Pow(TS_POWER_CONVERSION_NUMBER, denominatorBase2);
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
                double timeDelta = tickDelta / (double)loadedChartResolution * SECONDS_PER_MINUTE / bpms[i - 1];
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
            (ChartEventGroup.HeaderType)Enum.Parse(typeof(ChartEventGroup.HeaderType), cleanIdentifier)
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

        var dictionaryConversion = eventData.Select(line => line.Split(" = ", 2)).ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

        identifiedSection.data = dictionaryConversion;
        return identifiedSection;
    }

    static int GetResolution(string[] file)
    {
        for (int i = 0; i < file.Length; i++)
        {
            if (file[i].Contains("Resolution"))
            {
                var parts = file[i].Split(" = ");
                return int.Parse(parts[1].Trim());
            }
        }
        throw new ArgumentException("Chart does not contain a resolution. Please add the correct resolution to the file and try again.");
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
    public Dictionary<string, string> data;

    public ChartEventGroup(HeaderType identifier)
    {
        EventGroupIdentifier = identifier;
    }
} 
