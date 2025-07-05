using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class ChartParser : MonoBehaviour
{
    // Note: When creating new chart file, make sure [SyncTrack] starts with a BPM and TS declaration!
    const int DEFAULT_TS_DENOMINATOR = 4;

    public static int loadedChartResolution = UserSettings.DefaultResolution;

    static (List<int>, List<float>, SortedDictionary<int, TSData>) GetSyncTrackEvents(string filePath)
    {
        string[] chart = File.ReadAllLines(filePath);
        int syncTrackPos = Array.IndexOf(chart, "[SyncTrack]");

        loadedChartResolution = FindResolution(chart);

        if (syncTrackPos == -1)
        {
            throw new ArgumentException("No [SyncTrack] section found in file. Chart files require a [SyncTrack] section to be processed. Please check the file and try again.");
        }

        List<int> tempoTickTimeKeys = new();
        List<float> bpmVals = new();
        SortedDictionary<int, TSData> tsEvents = new();

        var lineIndex = syncTrackPos + 2; // + 2 because first line is {
        var currentLine = chart[lineIndex];
        while (lineIndex < chart.Length && !currentLine.Contains("}"))
        {
            if (!CheckTempoEventLegality(currentLine)) throw new ArgumentException("[SyncTrack] has invalid tempo event. Please check file and try again.");

            var dividerIndex = currentLine.IndexOf(" = ");
            var tickTimeKey = currentLine[..dividerIndex].Trim();
            var eventValue = currentLine[(dividerIndex + 2)..].Trim().Split(" ");

            // Next step based on if the event is a beat or time signature change (identifier in index 0 of the value)
            if (eventValue[0] == "B")
            {
                tempoTickTimeKeys.Add(int.Parse(tickTimeKey));
                bpmVals.Add(float.Parse(eventValue[1]) / 1000); // div by 1000 because .chart formats three-decimal BPM with no decimal point
            }
            else if(eventValue[0] == "TS")
            {
                string[] tsParts = eventValue[1].Split(" ");
                if (tsParts.Length == 1) // There is no space in the event value (only one number)
                {
                    tsEvents.Add(int.Parse(tickTimeKey), new TSData(int.Parse(eventValue[1]), DEFAULT_TS_DENOMINATOR)); // Add default TS denom
                }
                else
                {
                    // If there is a TS event with two parts, undo the log in the second term (refer to format specs)
                    tsEvents.Add(int.Parse(tickTimeKey), new TSData(int.Parse(tsParts[0]), (int)Math.Pow(2, int.Parse(tsParts[1]))));
                }
            }

            lineIndex++;
            currentLine = chart[lineIndex];
        }

        return (tempoTickTimeKeys, bpmVals, tsEvents);
    }

    /// <summary>
    /// Get a TempoEvent dictionary from a .chart file.
    /// </summary>
    /// <param name="filePath">The path of the .chart file.</param>
    /// <returns>The sorted dictionary of TempoEvent values.</returns>
    public static (SortedDictionary<int, BPMData>, SortedDictionary<int, TSData>) GetSyncTrackEventDicts(string filePath)
    {
        SortedDictionary<int, BPMData> outputTempoEventsDict = new();

        (var tickTimeKeys, var bpmVals, var tsEvents) = GetSyncTrackEvents(filePath);

        double currentSongTime = 0;
        for (int i = 0; i < tickTimeKeys.Count; i++) // Calculate time-second positions of tempo changes for beatline rendering
        {
            double calculatedTimeSecondDifference = 0;

            if (i > 0)
            {
                // Taken from Chart File Format Specifications -> Calculate time from one pos to the next at a constant bpm
                calculatedTimeSecondDifference = 
                (tickTimeKeys[i] - tickTimeKeys[i - 1]) / (double)loadedChartResolution * 60 / bpmVals[i - 1]; // 320 is sub-in for chart res right now b/c that's what i use personally
            }

            currentSongTime += calculatedTimeSecondDifference;
            outputTempoEventsDict.Add(tickTimeKeys[i], new BPMData(bpmVals[i], (float)currentSongTime));
        }

        return (outputTempoEventsDict, tsEvents);
    }

    static int FindResolution(string[] file)
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

    static bool CheckTempoEventLegality(string line)
    {
        if (line.Contains("B") || line.Contains("TS"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
