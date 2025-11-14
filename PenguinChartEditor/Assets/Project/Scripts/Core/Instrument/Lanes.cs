using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Lanes<T> where T : IEventData
{
    LaneSet<T>[] lanes;
    SelectionSet<T>[] selections;
    ClipboardSet<T>[] clipboards;
    public HashSet<int> TempSustainTicks = new();

    public Lanes(int laneCount)
    {
        lanes = new LaneSet<T>[laneCount];
        selections = new SelectionSet<T>[laneCount];
        clipboards = new ClipboardSet<T>[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new();
            selections[i] = new(lanes[i]);
            clipboards[i] = new(lanes[i]);
        }
    }

    public LaneSet<T> GetLane(int lane) => lanes[lane];
    public void SetLane(int lane, SortedDictionary<int, T> newData) => lanes[lane].Update(newData);
    public SelectionSet<T> GetLaneSelection(int lane) => selections[lane];
    public ClipboardSet<T> GetLaneClipboard(int lane) => clipboards[lane];

    public bool IsTickChord(int tick)
    {
        int noteCount = 0;
        for (int i = 0; i < Count; i++)
        {
            if (lanes[i].ContainsKey(tick)) noteCount++;
            if (noteCount >= 2) return true;
        }
        return false;
    }

    public List<int> UniqueTicks
    {
        get
        {
            HashSet<int> receiver = new();
            for (int i = 0; i < Count; i++)
            {
                receiver.UnionWith(lanes[i].Keys);
            }
            List<int> sortedTicks = new(receiver);
            sortedTicks.Sort();
            return sortedTicks;
        }
    }

    public int GetPreviousTickEvent(int tick)
    {
        var ticks = UniqueTicks;

        var index = ticks.BinarySearch(tick);
        if (index < 0) index = ~index;

        if (index == 0) return ticks[index];
        return ticks[index - 1];
    }

    public int GetNextTickEvent(int tick)
    {
        var ticks = UniqueTicks;
        var index = ticks.BinarySearch(tick);

        if (index < 0)
        {
            index = ~index;
            if (index == ticks.Count) return ticks[index - 1];
            return (ticks[index]);
        }

        return ticks[index + 1];
    }

    public int GetPreviousTickInLane(int lane, int tick)
    {
        var laneSet = lanes[lane].Keys.ToList();
        var index = laneSet.BinarySearch(tick);
        if (index < 0) throw new ArgumentException($"Tick {tick} does not exist in lane {lane}.");
        return laneSet[index - 1];
    }
    public int GetNextTickInLane(int lane, int tick)
    {
        var laneSet = lanes[lane].Keys.ToList();
        var index = laneSet.BinarySearch(tick);
        if (index < 0) throw new ArgumentException($"Tick {tick} does not exist in lane {lane}.");
        return laneSet[index + 1];
    }

    public int Count => lanes.Length;
}