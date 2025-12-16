using System.Collections.Generic;
using System.Linq;

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

    public delegate void UpdateNeededDelegate(int startTick, int endTick);

    /// <summary>
    /// Invoked whenever a hopo check needs to happen at a certain tick. 
    /// When invoked, the tick from the delegate should be checked to see if it or its surrounding ticks have changed hopo status.
    /// </summary>
    public event UpdateNeededDelegate UpdatesNeededInRange;

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

    public SortedDictionary<int, T>[] ExportNormalizedSelection()
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
        MinMaxTracker tracker = new(Count);

        for (int i = 0; i < Count; i++)
        {
            lanes[i].OverwriteDataWithOffset(newData[i], offset);
            if (newData[i].Count == 0) continue;
            var keys = newData[i].Keys;
            tracker.AddTickMinMax(keys.Min(), keys.Max());
        }

        var ticks = tracker.GetAbsoluteMinMax();
        UpdatesNeededInRange?.Invoke(ticks.min, ticks.max);
    }

    public void OverwriteTicksFromSet(SortedDictionary<int, T>[] newData, HashSet<int>[] ticks)
    {
        MinMaxTracker tracker = new(Count);
        for (int i = 0; i < Count; i++)
        {
            lanes[i].OverwriteTicksFromSet(ticks[i], newData[i]);
            tracker.AddTickMinMax(ticks[i].Min(), ticks[i].Max());
        }
        var endTicks = tracker.GetAbsoluteMinMax();
        UpdatesNeededInRange?.Invoke(endTicks.min, endTicks.max);
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

    public HashSet<int>[] GetTotalSelectionByLane()
    {
        HashSet<int>[] ticks = new HashSet<int>[Count];
        for (int i = 0; i < Count; i++)
        {
            ticks[i] = selections[i].GetSelectedTicks();
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

    public void RemoveTickFromTotalSelection(int tick)
    {
        for (int i = 0; i < Count; i++)
        {
            selections[i].Remove(tick);
        }
    }

    public void SelectAll()
    {
        for (int i = 0; i < Count; i++)
        {
            selections[i].SelectAllInLane();
        }
        Chart.Refresh();
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