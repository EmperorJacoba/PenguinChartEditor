using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Tempo
{
    const int SECONDS_PER_MINUTE = 60;
    const string ANCHOR_IDENTIFIER = "A";
    public const int MICROSECOND_CONVERSION = 1000000;
    const float MINIMUM_BPM_VALUE = 0;
    const float MAXIMUM_BPM_VALUE = 1000;

    public static LaneSet<BPMData> Events { get; set; }

    public static List<string> ExportAllEvents()
    {
        List<string> eventContainer = new(Events.Count);
        foreach (var @event in Events)
        {
            eventContainer.Add
                (
                    $"\t{@event.Key} = {@event.Value.ToChartFormat(0)}"
                );

            if (@event.Value.Anchor)
            {
                eventContainer.Add($"{@event.Key} = {ANCHOR_IDENTIFIER} {@event.Value.Timestamp * MICROSECOND_CONVERSION}");
            }
        }
        return eventContainer;
    }

    public static int GetNextTempoEventExclusive(int currentTick)
    {
        var tickTimeKeys = Events.Keys.ToList();

        var index = tickTimeKeys.BinarySearch(currentTick);

        // modify index if the found timestamp is at the end of the array (last tempo event)
        if (~index == tickTimeKeys.Count) return tickTimeKeys.Count - 1;

        // bitwise complement is negative
        if (index > 0) return tickTimeKeys[index + 1];

        // else just get the index proper 
        else index = ~index + 1;
        try
        {
            return tickTimeKeys[index];
        }
        catch
        {
            return tickTimeKeys[0]; // if ~index - 1 is -1, then the index should be itself
        }
    }

    /// <summary>
    /// Find the last tempo event before a specified tick. WILL NOT return the passed in tick if an event exists at that position, and will instead return the true last event.
    /// </summary>
    /// <param name="currentTick"></param>
    /// <returns></returns>
    public static int GetLastTempoEventTickExclusive(int currentTick)
    {
        var tickTimeKeys = Events.Keys.ToList();

        var index = tickTimeKeys.BinarySearch(currentTick);

        // bitwise complement is negative
        if (index > 0) return tickTimeKeys[index - 1]; // exclusivity happens here

        // modify index if the found timestamp is at the end of the array (last tempo event)
        if (~index == tickTimeKeys.Count) index = tickTimeKeys.Count - 1;
        // else just get the index proper 
        else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before to properly render beatlines
        try
        {
            return tickTimeKeys[index];
        }
        catch
        {
            return tickTimeKeys[0]; // if ~index - 1 is -1, then the index should be itself
        }
    }

    /// <summary>
    /// Find the last tempo event before a specified tick. Can return the passed in tick if an event exists at that position.
    /// </summary>
    /// <param name="currentTick"></param>
    /// <returns>The tick-time timestamp of the previous tempo event.</returns>
    public static int GetLastTempoEventTickInclusive(int currentTick)
    {
        if (currentTick < 0) return 0;
        var tickTimeKeys = Events.Keys.ToList();

        var index = tickTimeKeys.BinarySearch(currentTick);

        if (index >= 0) return tickTimeKeys[index]; // bitwise complement is negative

        // modify index if the found timestamp is at the end of the array (last tempo event)
        if (~index == tickTimeKeys.Count) index = tickTimeKeys.Count - 1;
        // else just get the index proper 
        else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before

        return index > 0 ? tickTimeKeys[index] : tickTimeKeys[0];
    }

    public static int GetNextAnchor(int currentTick)
    {
        var nextAnchors = Events.Where(item => item.Key > currentTick && item.Value.Anchor).ToList();
        if (nextAnchors.Count > 0) return nextAnchors[0].Key;
        else return -1;
    }

    public static int GetLastAnchor(int currentTick)
    {
        var lastAnchors = Events.Where(item => item.Key < currentTick && item.Value.Anchor).ToList();
        if (lastAnchors.Count > 0) return lastAnchors[^1].Key;
        else return -1;
    }

    // This anchoring logic may present some accuracy errors in the dictionary
    // *should* only be microseconds at most but logic may need to be rethought if possible
    // maybe re-validate dictionary when exporting?
    // Effects currently unknown, but round off should fix it if anything
    // Please remove and re-think if any errors arise from exporting to different software/YARG/Clone Hero
    // NO EVIDENCE FOR INACCURACY AT THIS TIME
    public static float CalculateLastBPMBeforeAnchor(int currentTick, float newTime)
    {
        var nextAnchor = GetNextAnchor(currentTick);
        return CalculateLastBPMBeforeAnchor(currentTick, newTime, nextAnchor);
    }

    public static float CalculateLastBPMBeforeAnchor(int currentTick, float newTime, int nextAnchor)
    {
        float anchoredBPS = ((nextAnchor - currentTick) / (float)Chart.Resolution) / (Events[nextAnchor].Timestamp - newTime);
        float anchoredBPM = (float)Math.Round((anchoredBPS * 60), 3);
        return anchoredBPM;
    }

    // BPM can't be negative and event selection gets screwed with when the BPM is too high
    public static bool IsTickInBounds(float bpm) => bpm > MINIMUM_BPM_VALUE && bpm < MAXIMUM_BPM_VALUE;

    public static void RecalculateTempoEventDictionary(int modifiedTick, float timeChange)
    {
        SortedDictionary<int, BPMData> outputTempoEventsDict = new();

        var tickEvents = Events.Keys.ToList();
        var positionOfTick = tickEvents.FindIndex(x => x == modifiedTick);
        if (positionOfTick == tickEvents.Count - 1) return; // no events to modify

        // Keep all events before change when creating new dictionary
        // Manage anchors in BPMLabel.ChangePositionFromDrag() - much simpler
        for (int i = 0; i <= positionOfTick; i++)
        {
            outputTempoEventsDict.Add(tickEvents[i], Events[tickEvents[i]]);
        }

        // Start new data with the song timestamp of the change
        double currentSongTime = outputTempoEventsDict[tickEvents[positionOfTick]].Timestamp;
        for (int i = positionOfTick + 1; i < tickEvents.Count; i++)
        {
            var bpmChange = Events[tickEvents[i]].BPMChange;

            if (tickEvents.Count > (i + 1)) // validation check - no anchor will be ahead of the last event
            {
                // anchor calculations happen on the bpm event before an anchor, 
                // so instead of catching the anchor when we get to it, catch it before to avoid multiple writes to the same index
                if (Events[tickEvents[i + 1]].Anchor)
                {
                    bpmChange = CalculateLastBPMBeforeAnchor(tickEvents[i], Events[tickEvents[i]].Timestamp + timeChange);
                }
            }

            // anchor = time no changey
            if (Events[tickEvents[i]].Anchor)
            {
                timeChange = 0;
            }

            outputTempoEventsDict.Add(tickEvents[i], new BPMData(bpmChange, Events[tickEvents[i]].Timestamp + timeChange, Events[tickEvents[i]].Anchor));
        }

        Events.Update(outputTempoEventsDict);
    }

    /// <summary>
    /// Recalculate all tempo events from the tick-time timestamp modified onward.
    /// </summary>
    /// <param name="modifiedTick">The last tick modified to update all future ticks from.</param>
    public static void RecalculateTempoEventDictionary(int modifiedTick = 0)
    {
        SortedDictionary<int, BPMData> outputTempoEventsDict = new();

        var tickEvents = Events.Keys.ToList();
        var positionOfTick = tickEvents.FindIndex(x => x == modifiedTick);
        if (positionOfTick == tickEvents.Count - 1) return; // no events to modify

        // Keep all events before change when creating new dictionary
        for (int i = 0; i <= positionOfTick; i++)
        {
            outputTempoEventsDict.Add(tickEvents[i], Events[tickEvents[i]]);
        }
        // Start new data with the song timestamp of the change
        double currentSongTime = outputTempoEventsDict[tickEvents[positionOfTick]].Timestamp;
        for (int i = positionOfTick + 1; i < tickEvents.Count; i++)
        {
            double calculatedTimeSecondDifference = 0;

            if (i > 0)
            {
                // Taken from Chart File Format Specifications -> Calculate time from one pos to the next at a constant bpm
                calculatedTimeSecondDifference =
                (tickEvents[i] - tickEvents[i - 1]) / (double)Chart.Resolution * 60 / Events[tickEvents[i - 1]].BPMChange;
            }

            currentSongTime += calculatedTimeSecondDifference;
            outputTempoEventsDict.Add(tickEvents[i], new BPMData(Events[tickEvents[i]].BPMChange, (float)currentSongTime, Events[tickEvents[i]].Anchor));
        }

        Events.Update(outputTempoEventsDict);
    }

    /// <summary>
    /// Take a number of seconds (in S.ms form - ex. 61.1 seconds) and convert it to MM:SS.mmm format (where 61.1 returns 01:01.100)
    /// </summary>
    /// <param name="position">The unformatted second count.</param>
    /// <returns>The formatted MM:SS:mmm timestamp of the second position</returns>
    public static string ConvertSecondsToTimestamp(double position)
    {
        var minutes = Math.Floor(position / 60);
        var secondsWithMS = position - minutes * 60;
        var seconds = (int)Math.Floor(secondsWithMS);
        var milliseconds = Math.Round(secondsWithMS - seconds, 3) * 1000;

        string minutesString = minutes.ToString();
        if (minutes < 10)
        {
            minutesString = minutesString.PadLeft(minutesString.Length + 1, '0');
        }

        string secondsString = seconds.ToString();
        if (seconds < 10)
        {
            secondsString = secondsString.PadLeft(2, '0');
        }

        string millisecondsString = milliseconds.ToString();
        if (millisecondsString.Length < 3)
        {
            millisecondsString = millisecondsString.PadRight(3, '0');
        }

        return minutesString + ":" + secondsString + "." + millisecondsString;
    }

    public static int ConvertSecondsToTickTime(float timestamp)
    {
        if (timestamp <= 0)
            return 0;

        else if (timestamp > AudioManager.SongLength)
            return SongTime.SongLengthTicks;

        // Get parallel lists of the tick-time events and time-second values so that value found with seconds can be converted to a tick-time event
        var tempoTickTimeEvents = Events.Keys.ToList();
        var tempoTimeSecondEvents = Events.Values.Select(x => x.Timestamp).ToList();

        // Attempt a binary search for the current timestamp, 
        // which will return a bitwise complement of the index of the next highest timesecond value 
        // OR tempoTimeSecondEvents.Count if there are no more elements
        var index = tempoTimeSecondEvents.BinarySearch(timestamp);

        int lastTickEvent;
        if (index <= 0) // bitwise complement is negative or zero
        {
            // modify index if the found timestamp is at the end of the array (last tempo event)
            if (~index == tempoTimeSecondEvents.Count) index = tempoTimeSecondEvents.Count - 1;
            // else just get the index proper 
            else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before to properly render beatlines
            try
            {
                lastTickEvent = tempoTickTimeEvents[index];
            }
            catch
            {
                lastTickEvent = tempoTickTimeEvents[0]; // if ~index - 1 is -1, then the index should be itself
            }
        }
        else
        {
            lastTickEvent = tempoTickTimeEvents[index];
        }

        // Rearranging of .chart format specification distance between two ticks - thanks, algebra class!
        return Mathf.RoundToInt((Chart.Resolution * Events[lastTickEvent].BPMChange * (float)(timestamp - Events[lastTickEvent].Timestamp) / SECONDS_PER_MINUTE) + lastTickEvent);
    }

    public static double ConvertTickTimeToSeconds(int ticktime)
    {
        var lastTickEvent = GetLastTempoEventTickInclusive(ticktime);
        // Formula from .chart format specifications
        return ((ticktime - lastTickEvent) / (double)Chart.Resolution * SECONDS_PER_MINUTE / Events[lastTickEvent].BPMChange) + Events[lastTickEvent].Timestamp;
    }
    
    public static void SetEvents(SortedDictionary<int, BPMData> newEvents)
    {
        var breakKey = GetFirstVariableEvent(newEvents);
        Events.Update(newEvents);

        if (!Events.ContainsKey(0))
        {
            Events.Add(0, new BPMData(Chart.SyncTrackInstrument.bpmMoveData.currentMoveAction.poppedData[0].BPMChange, 0, Chart.SyncTrackInstrument.bpmMoveData.currentMoveAction.poppedData[0].Anchor));
        }

        // Safety check before recalculating dictionary
        // When pasting into dictionary, tick 0 might inherit data
        // from a pasted event that has a timestamp which is not 0.
        // Tick 0's timestamp is always 0
        if (Events[0].Timestamp != 0)
        {
            Events[0] = new BPMData(Events[0].BPMChange, 0, Events[0].Anchor);
        }

        if (breakKey != -1)
        {
            RecalculateTempoEventDictionary(GetLastTempoEventTickExclusive(breakKey));
        }
    }

    public static int GetFirstVariableEvent(SortedDictionary<int, BPMData> newData)
    {
        var currentKeys = Events.Keys.ToHashSet();
        currentKeys.UnionWith(newData.Keys.ToHashSet());
        currentKeys.OrderBy(x => x);

        foreach (var key in currentKeys)
        {
            try
            {
                if (newData[key] != Events[key]) // Data has been edited at this point
                {
                    return key;
                }
            }
            catch // The key cannot be accessed in one of the dictionaries (addition/removal)
            {
                return key;
            }
        }
        return -1; // No discrepency
    }
}