using System.Collections.Generic;
using System.Linq;

public class MinMaxTracker
{
    HashSet<int> minTicks;
    HashSet<int> maxTicks;

    public MinMaxTracker(int laneCount)
    {
        minTicks = new(laneCount - 1);
        maxTicks = new(laneCount - 1);
    }

    public void AddTickMinMax(int min, int max)
    {
        minTicks.Add(min);
        maxTicks.Add(max);
    }

    public MinMaxTicks GetAbsoluteMinMax()
    {
        
        return new(minTicks.Min(), maxTicks.Max());
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