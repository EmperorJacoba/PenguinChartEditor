using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.UI;

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

    public int GetTickCountAtTick(int tick)
    {
        int noteCount = 0;
        for (int i = 0; i < Count; i++)
        {
            if (lanes[i].ContainsKey(tick)) noteCount++;
        }
        return noteCount;
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

    public HashSet<int> GetUniqueTicksInRange(int startTick, int endTick)
    {
        throw new NotImplementedException();
    }

    public bool AnyLaneContainsTick(int tick)
    {
        for (int i = 0; i < Count; i++)
        {
            if (lanes[i].Contains(tick)) return true;
        }
        return false;
    }

    public const int NO_TICK_EVENT = -1;

    public TickBounds GetTickEventBounds(int tick)
    {
        var ticks = UniqueTicks;
        int prev;
        int next;

        var index = ticks.BinarySearch(tick);
        if (index < 0)
        {
            index = ~index;

            next = index == ticks.Count ? NO_TICK_EVENT : ticks[index];
        }
        else
        {
            next = ticks.Count > index + 1 ? ticks[index + 1] : NO_TICK_EVENT;
        }
        prev = index == 0 ? NO_TICK_EVENT : ticks[index - 1];

        return new TickBounds(prev, next);
    }

    public HashSet<int> GetTotalSelection()
    {
        HashSet<int> ticks = new();
        for (int i = 0; i < Count; i++)
        {
            ticks.UnionWith(selections[i]);
        }

        return ticks;
    }

    public void ClearAllSelections()
    {
        for (int i = 0; i < Count; i++)
        {
            selections[i].Clear();
        }
    }
    public int Count => lanes.Length;
}

public struct TickBounds
{
    public readonly int prev;
    public readonly int next;

    public TickBounds(int prev, int next)
    {
        this.prev = prev;
        this.next = next;
    }
}