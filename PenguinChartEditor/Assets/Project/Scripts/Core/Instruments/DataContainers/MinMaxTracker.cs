using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MinMaxTracker
{
    private HashSet<int> minTicks;
    private HashSet<int> maxTicks;

    public MinMaxTracker(int laneCount)
    {
        minTicks = new HashSet<int>(laneCount - 1);
        maxTicks = new HashSet<int>(laneCount - 1);
    }

    public void AddTickMinMax(int min, int max)
    {
        minTicks.Add(min);
        maxTicks.Add(max);
    }

    public MinMaxTicks GetAbsoluteMinMax()
    {
        if (minTicks.Count == 0 || maxTicks.Count == 0)
        {
            return new MinMaxTicks(0, SongTime.SongLengthTicks);
        }
        return new MinMaxTicks(minTicks.Min(), maxTicks.Max());
    }
}

public struct MinMaxTicks
{
    public readonly int min;
    public readonly int max;

    public MinMaxTicks(int min, int max)
    {
        this.min = min;
        this.max = max;
    }
}