using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class Lanes<T> where T : IEventData
{
    LaneSet<T>[] lanes;
    SelectionSet<T>[] selections;
    public HashSet<int> TempSustainTicks = new();

    public Lanes(int laneCount)
    {
        lanes = new LaneSet<T>[laneCount];
        selections = new SelectionSet<T>[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new();
            selections[i] = new(lanes[i]);
        }
    }

    public LaneSet<T> GetLane(int lane) => lanes[lane];
    public void SetLane(int lane, SortedDictionary<int, T> newData) => lanes[lane].Update(newData);
    public SelectionSet<T> GetLaneSelection(int lane) => selections[lane];

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

    public int GetFirstSelectionTick()
    {
        HashSet<int> minSelectionTicks = new();
        for (int i = 0; i < Count; i++)
        {
            if (selections[i].Count > 0) minSelectionTicks.Add(selections[i].Min());
        }
        return minSelectionTicks.Count > 0 ? minSelectionTicks.Min() : SelectionSet<T>.NONE_SELECTED;
    }

    public SortedDictionary<int, T>[] ExportNormalizedSelectionData()
    {
        SortedDictionary<int, T>[] normalizedData = new SortedDictionary<int, T>[Count];
        for (int i = 0; i < Count; i++)
        {
            normalizedData[i] = selections[i].ExportNormalizedData(GetFirstSelectionTick());
        }
        // need code here to add relative lane-by-lane offset to preserve lane-by-lane spacing
        return normalizedData;
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

    public SortedDictionary<int, T>[] ExportData()
    {
        SortedDictionary<int, T>[] saveData = new SortedDictionary<int, T>[lanes.Length];
        for (int i = 0; i < Count; i++)
        {
            saveData[i] = lanes[i].ExportData();
        }
        return saveData;
    }

    public void SetLaneData(SortedDictionary<int, T>[] newData)
    {
        for (int i = 0; i < Count; i++)
        {
            lanes[i].OverwriteLaneDataWith(newData[i]);
        }    
    }

    public void OverwriteLaneDataWithOffset(SortedDictionary<int, T>[] newData, int offset)
    {
        for (int i = 0; i < Count; i++)
        {
            lanes[i].OverwriteDataWithOffset(newData[i], offset);
        }
        
        // need to do this separately because the invoke within the method above will not take into account
        // new data from lanes not yet added
        // maybe add a check to disable doing this if not needed?
        for (int i = 0; i < Count; i++)
        {
            lanes[i].InvokeForSetEnds(newData[i], offset);
        }
    }

    public void OverwriteTicksFromSet(SortedDictionary<int, T>[] newData, HashSet<int>[] ticks)
    {
        for (int i = 0; i < Count; i++)
        {
            lanes[i].OverwriteTicksFromSet(ticks[i], newData[i]);
        }
    }

    public void ApplyScaledSelection(SortedDictionary<int, T>[] movingData, int lastPasteStartTick)
    {
        for (int i = 0; i < Count; i++)
        {
            selections[i].ApplyScaledSelection(movingData[i], lastPasteStartTick);
        }
    }

    public SortedDictionary<int, T>[] PopDataInRange(int startTick, int endTick)
    {
        SortedDictionary<int, T>[] subtractedData = new SortedDictionary<int,T>[lanes.Length];
        for (int i = 0; i < Count; i++)
        {
            subtractedData[i] = lanes[i].PopTicksInRange(startTick, endTick);
        }
        return subtractedData;
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

    public void DeleteAllTicksInSelection()
    {
        for (int i = 0; i < Count; i++)
        {
            selections[i].PopSelectedTicksFromLane();
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