using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChartParser : MonoBehaviour
{
    // Note: When creating new chart file, make sure [SyncTrack] starts with a BPM and TS declaration!
    
    static (List<int>, List<float>) GetChartTempoEvents(string filePath)
    {
        StreamReader chart = new(filePath);

        var line = chart.ReadLine();
        while (line != null) // Loop through lines until tempo events are found
        {
            if (line == "[SyncTrack]")
            {
                break;
            }
            line = chart.ReadLine();
        }
        // User didn't pass in a properly formatted chart file
        if (line == null) throw new ArgumentException("Could not find [SyncTrack] in file. Please try again.");

        // line after [SyncTrack] should have a curly brace open 
        line = chart.ReadLine();
        if (line != "{") throw new ArgumentException("[SyncTrack] is not properly enclosed. Please check file and try again.");

        // Set up lists to deposit info into
        // This is not a dictionary because time-second calculations
        // are needed so save dictionary compilation until final export 
        List<int> tickTimeKeys = new();
        List<float> bpmVals = new();
        // Need pairing TS lists


        line = chart.ReadLine(); // Get into the meat of [SyncTrack]
        while (!line.Contains("}")) // needs to be "Contains" -> Moonscraper generates "} " at the end of SyncTrack files
        {
            if (!CheckTempoEventLegality(line)) throw new ArgumentException("[SyncTrack] has invalid tempo event. Please check file and try again.");

            // Split into key and value pair to put different data types into different lists
            var parts = line.Split(" = ");

            // Get ready for list input -> get rid of spaces
            parts[0].Trim(); parts[1].Trim();
            var tickTimeKey = parts[0];

            // Split into identifier and value
            var eventValue = parts[1].Split(" ");

            // Next step based on if the event is a beat or time signature change (identifier in index 0 of the value)
            if (eventValue[0] == "B")
            {
                tickTimeKeys.Add(int.Parse(tickTimeKey));
                bpmVals.Add(float.Parse(eventValue[1]) / 1000); // div by 1000 because .chart formats three-decimal BPM with no decimal point
            }
            else if(eventValue[0] == "TS")
            {
                // TS logic here
            }
            line = chart.ReadLine();
        }

        return (tickTimeKeys, bpmVals);
    }

    /// <summary>
    /// Get a TempoEvent dictionary from a .chart file.
    /// </summary>
    /// <param name="filePath">The path of the .chart file.</param>
    /// <returns>The sorted dictionary of TempoEvent values.</returns>
    public static SortedDictionary<int, (float, float)> GetTempoEventDict(string filePath)
    {
        SortedDictionary<int, (float, float)> outputDict = new();

        (var tickTimeKeys, var bpmVals) = GetChartTempoEvents(filePath);

        float currentSongTime = 0;
        for (int i = 0; i < tickTimeKeys.Count; i++) // Calculate time-second positions of tempo changes for beatline rendering
        {
            float calculatedTimeSecondDifference = 0;
            try
            {
                // Taken from Chart File Format Specifications -> Calculate time from one pos to the next at a constant bpm
                calculatedTimeSecondDifference = 
                (tickTimeKeys[i] - tickTimeKeys[i - 1]) / 320 * 60 / bpmVals[i - 1]; // 320 is sub-in for chart res right now b/c that's what i use personally
            }
            catch
            {
                calculatedTimeSecondDifference = 0; // avoid OOB error for first event
            }
            currentSongTime += calculatedTimeSecondDifference;
            outputDict.Add(tickTimeKeys[i], (bpmVals[i], currentSongTime));
        }
        return outputDict;
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
