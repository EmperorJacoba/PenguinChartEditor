using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public static class TimeSignature
{
    const string SEPARATOR = " = ";
    const string TS_IDENTIFIER = "TS";
    public static LaneSet<TSData> Events { get; set; }
    public static void SetEvents(SortedDictionary<int, TSData> newEvents)
    {
        if (!newEvents.ContainsKey(0))
        {
            newEvents.Add(0, Chart.SyncTrackInstrument.tsMoveData.currentMoveAction.poppedData[0]);
        }
        Events.Update(newEvents);
    }

    public static List<string> ExportAllEvents()
    {
        List<string> eventContainer = new(Events.Count);
        foreach (var @event in Events)
        {
            string tick = $"{@event.Key}";

            string denom;

            if (@event.Value.Denominator == 4) denom = "";
            else denom = $" {Math.Log(@event.Value.Denominator, 2)}";

            string value = $"{TS_IDENTIFIER} {@event.Value.Numerator}{denom}"; // denom will contain leading space if needed

            string output = $"\t{tick}{SEPARATOR}{value}";
            eventContainer.Add(output);
        }
        return eventContainer;
    }

    /// <summary>
    /// Calculate the type of barline a specified tick-time position should be.
    /// </summary>
    /// <param name="beatlineTickTimePos"></param>
    /// <param name="inclusive">Use only if you want to calculate a predicted TS beatline, like when checking if the position of a TS event is on a barline based on its prior TS event.</param>
    /// <returns>The type of beatline at this tick.</returns>
    public static Beatline.BeatlineType CalculateBeatlineType(int beatlineTickTimePos, bool inclusive = true)
    {
        if (beatlineTickTimePos == 0) return Beatline.BeatlineType.barline;

        int lastTSTickTimePos;
        if (inclusive)
        {
            lastTSTickTimePos = GetLastTSEventTick(beatlineTickTimePos);
        }
        else
        {
            lastTSTickTimePos = GetLastTSEventTick(beatlineTickTimePos - 1);
        }

        var tsDiff = beatlineTickTimePos - lastTSTickTimePos; // need absolute distance between the current tick and the origin of the TS event

        // if the difference is divisible by the # of first-division notes in a bar, it's a barline
        if (tsDiff % (Chart.Resolution * (float)Events[lastTSTickTimePos].Numerator / (float)(Events[lastTSTickTimePos].Denominator / 4.0f)) == 0)
        {
            return Beatline.BeatlineType.barline;
        }
        // if it's divisible by the first-division, it's a division line
        else if (tsDiff % (Chart.Resolution / (float)Events[lastTSTickTimePos].Denominator * 4) == 0)
        {
            return Beatline.BeatlineType.divisionLine;
        }
        else if (tsDiff % (Chart.Resolution / ((float)Events[lastTSTickTimePos].Denominator * 2)) == 0)
        {
            return Beatline.BeatlineType.halfDivisionLine;
        }
        return Beatline.BeatlineType.none;
    }

    /// <summary>
    /// Calculate the last time signature event that occurs before a specified tick.
    /// </summary>
    /// <param name="currentTick"></param>
    /// <returns>The tick-time timestamp of the last time signature event.</returns>
    public static int GetLastTSEventTick(int currentTick)
    {
        var tsEvents = Events.Keys.ToList();

        var index = tsEvents.BinarySearch(currentTick);

        int ts;
        if (index < 0) // bitwise complement is negative
        {
            // modify index if the found timestamp is at the end of the array (last tempo event)
            if (~index == tsEvents.Count) index = tsEvents.Count - 1;
            // else just get the index proper 
            else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before to properly render beatlines
            try
            {
                ts = tsEvents[index];
            }
            catch
            {
                ts = tsEvents[0]; // if ~index - 1 is -1, then the index should be itself
            }
        }
        else
        {
            ts = tsEvents[index];
        }
        return ts;
    }

    /// <summary>
    /// Calculate the last "1" of a bar from a tick-time timestamp.
    /// </summary>
    /// <param name="currentTick">The tick-time timestamp to evaluate from.</param>
    /// <returns>The tick-time timestamp of the last barline.</returns>
    public static int GetLastBarline(int currentTick)
    {
        var ts = GetLastTSEventTick(currentTick);
        var tickDiff = currentTick - ts;
        var tickInterval = (Chart.Resolution * (float)Events[ts].Numerator) / ((float)Events[ts].Denominator / 4);
        int numIntervals = (int)Math.Floor(tickDiff / tickInterval); // floor is to snap it back to the minimum interval (get LAST barline, not closest)

        return (int)(ts + numIntervals * tickInterval);
    }

    /// <summary>
    /// Calculate the next "1" of a bar from a tick-time timestamp.
    /// </summary>
    /// <param name="currentTick">The tick-time timestamp to evaluate from.</param>
    /// <returns>The tick-time timestamp of the nextd barline.</returns>
    public static int GetNextBarline(int currentTick)
    {
        var ts = GetLastTSEventTick(currentTick);
        var tickDiff = currentTick - ts;
        var tickInterval = (Chart.Resolution * (float)Events[ts].Numerator) / ((float)Events[ts].Denominator / 4);
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        return (int)(ts + numIntervals * tickInterval);
    }

    /// <summary>
    /// Calculate the next beatline to be generated from a specified tick-time timestamp.
    /// </summary>
    /// <param name="currentTick"></param>
    /// <returns>The tick-time timestamp of the next beatline event.</returns>
    public static int GetNextBeatlineEvent(int currentTick)
    {
        var ts = GetLastTSEventTick(currentTick);
        var tickDiff = currentTick - ts;
        var tickInterval = Chart.Resolution / ((float)Events[ts].Denominator / 2);
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        return (int)(ts + numIntervals * tickInterval);
    }

    public static int GetNextBeatlineEventExclusive(int currentTick)
    {
        currentTick++;
        var ts = GetLastTSEventTick(currentTick);
        var tickDiff = (currentTick - ts);
        var tickInterval = Chart.Resolution / ((float)Events[ts].Denominator / 2);
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        var proposedNext = (int)(ts + numIntervals * tickInterval);
        var middleTSEvent = GetLastTSEventTick(proposedNext);

        // edge case where a new TS event falls within the calculated next event and current tick
        // happens if a TS event is placed on a non-beatline - that new TS has to be the next barline
        // this is only something that applies during testing stage - this is important tho
        if (middleTSEvent != GetLastTSEventTick(currentTick))
        {
            return middleTSEvent;
        }
        return proposedNext;
    }

    public static int GetNextDivisionEvent(int currentTick)
    {
        var ts = GetLastTSEventTick(currentTick);
        var tickDiff = currentTick - ts;
        var tickInterval = Chart.Resolution / ((float)Events[ts].Denominator / 4);
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        return (int)(ts + numIntervals * tickInterval);
    }

    // Call in CheckForEvents
    public static bool IsEventValid(int tick)
    {
        if (CalculateBeatlineType(tick, false) != Beatline.BeatlineType.barline)
        {
            return false;
        }
        else return true;
        // Every time event is placed run this check for all future events and put alert on scrubber
    }
    
    /// <summary>
    /// Calculate the amount of divisions are needed from the chart resolution for each first-division event.
    /// <para>Example: TS = 4/4 -> Returns 1, because chart resolution will need to be divided by 1 to reach the number of ticks between first-division (in this case quarter note) events.</para>
    /// <para>Example: TS = 3/8 -> Returns 2, because res will need to be div by 2 to reach eighth note events.</para>
    /// <para>Example: TS = 2/2 -> Returns 0.5</para>
    /// </summary>
    /// <param name="tick"></param>
    /// <returns>The factor to multiply the chart resolution by to get the first-division tick-time.</returns>
    public static float CalculateDivision(int tick)
    {
        int tsTick = GetLastTSEventTick(tick);
        return (float)Events[tsTick].Denominator / 4;
    }

    public static int IncreaseByHalfDivision(int tick)
    {
        return (int)(Chart.Resolution / CalculateDivision(tick) / 2);
    }
}