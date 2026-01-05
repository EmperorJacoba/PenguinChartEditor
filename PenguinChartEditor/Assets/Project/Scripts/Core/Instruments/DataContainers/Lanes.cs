using Penguin.Debug;
using System.Collections.Generic;
using System.Linq;

public class Lanes<T> where T : IEventData
{
    public const int NO_TICK_EVENT = -1;
    public List<int> LaneKeys => lanes.Keys.ToList();
    Dictionary<int, LaneSet<T>> lanes;
    Dictionary<int, SelectionSet<T>> selections;
    public HashSet<int> TempSustainTicks = new();

    public Lanes(int laneCount)
    {
        lanes = new();
        selections = new();

        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new();
            selections[i] = new(lanes[i]);
        }
    }

    public Lanes(List<int> laneIDs)
    {
        lanes = new();
        selections = new();

        foreach (var id in laneIDs)
        {
            lanes[id] = new();
            selections[id] = new(lanes[id]);
        }
    }

    public delegate void UpdateNeededDelegate(int startTick, int endTick);

    /// <summary>
    /// Invoked whenever a hopo check needs to happen at a certain tick. 
    /// When invoked, the tick from the delegate should be checked to see if it or its surrounding ticks have changed hopo status.
    /// </summary>
    public event UpdateNeededDelegate UpdatesNeededInRange;

    Dictionary<int, SortedDictionary<int, T>> MakeEmptyDataSet()
    {
        Dictionary<int, SortedDictionary<int, T>> outputSet = new();
        foreach (var set in lanes)
        {
            outputSet[set.Key] = new();
        }
        return outputSet;
    }

    public LaneSet<T> GetLane(int lane) => lanes[lane];
    public void SetLane(int lane, SortedDictionary<int, T> newData) => lanes[lane].Update(newData);
    public SelectionSet<T> GetLaneSelection(int lane) => selections[lane];

    public bool IsTickChord(int tick)
    {
        int noteCount = 0;
        foreach (var lane in lanes.Values)
        {
            if (lane.Contains(tick)) noteCount++;
            if (noteCount >= 2) return true;
        }
        return false;
    }

    public int GetTickCountAtTick(int tick)
    {
        int noteCount = 0;
        foreach (var lane in lanes.Values)
        {
            if (lane.Contains(tick)) noteCount++;
        }
        return noteCount;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Sorted List of all ticks present in the lane data defined in this object.</returns>
    public List<int> GetUniqueTickSet()
    {
        HashSet<int> receiver = new();
        foreach (var lane in lanes.Values)
        {
            receiver.UnionWith(lane.Keys);
        }
        List<int> sortedTicks = new(receiver);
        sortedTicks.Sort();
        return sortedTicks;
    }

    public int GetFirstSelectionTick()
    {
        HashSet<int> minSelectionTicks = new();
        foreach (var selection in selections.Values)
        {
            if (selection.Count > 0) minSelectionTicks.Add(selection.Min());
        }
        return minSelectionTicks.Count > 0 ? minSelectionTicks.Min() : SelectionSet<T>.NONE_SELECTED;
    }

    public MinMaxTicks GetSelectionBounds()
    {
        MinMaxTracker minMaxTracker = new(Count);

        foreach (var selection in selections.Values)
        {
            if (selection.Count > 0) minMaxTracker.AddTickMinMax(selection.Min(), selection.Max());
        }

        return minMaxTracker.GetAbsoluteMinMax();
    }

    public Dictionary<int, SortedDictionary<int, T>> ExportNormalizedSelection()
    {
        var normalizedOutputSet = MakeEmptyDataSet();
        var firstSelectionTick = GetFirstSelectionTick();
        foreach (var selection in selections)
        {
            normalizedOutputSet[selection.Key] = selection.Value.ExportNormalizedData(firstSelectionTick);
        }
        return normalizedOutputSet;
    }

    public bool AnyLaneContainsTick(int tick)
    {
        foreach (var lane in lanes.Values)
        {
            if (lane.Contains(tick)) return true;
        }
        return false;
    }

    public TickBounds GetTickEventBounds(int tick)
    {
        var ticks = GetUniqueTickSet();

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

    public Dictionary<int, SortedDictionary<int, T>> ExportData()
    {
        var exportedData = MakeEmptyDataSet();
        foreach (var lane in lanes)
        {
            exportedData[lane.Key] = lane.Value.ExportData();
        }
        return exportedData;
    }

    public void OverwriteLaneData(Dictionary<int, SortedDictionary<int, T>> newData)
    { 
        foreach (var newDataLane in newData)
        {
            lanes[newDataLane.Key].OverwriteLaneDataWith(newDataLane.Value);
        }
    }

    public void OverwriteLaneDataWithOffset(Dictionary<int, SortedDictionary<int, T>> newData, int offset)
    {
        MinMaxTracker tracker = new(Count);

        foreach(var newDataLane in newData)
        {
            if (newDataLane.Value.Count == 0) continue;

            lanes[newDataLane.Key].OverwriteDataWithOffset(newDataLane.Value, offset);
            var keys = newDataLane.Value.Keys;
            tracker.AddTickMinMax(keys.Min(), keys.Max());
        }

        var ticks = tracker.GetAbsoluteMinMax();
        UpdatesNeededInRange?.Invoke(ticks.min, ticks.max);
    }

    public void OverwriteTicksFromSet(Dictionary<int, SortedDictionary<int, T>> newData, Dictionary<int, HashSet<int>> ticks)
    {
        MinMaxTracker tracker = new(Count);
        foreach (var newDataLane in newData)
        {
            if (ticks[newDataLane.Key].Count == 0) continue;

            lanes[newDataLane.Key].OverwriteTicksFromSet(ticks[newDataLane.Key], newDataLane.Value);
            tracker.AddTickMinMax(ticks[newDataLane.Key].Min(), ticks[newDataLane.Key].Max());
        }
        var endTicks = tracker.GetAbsoluteMinMax();
        UpdatesNeededInRange?.Invoke(endTicks.min, endTicks.max);
    }

    public void ApplyScaledSelection(Dictionary<int, SortedDictionary<int, T>> movingData, int lastPasteStartTick)
    {
        foreach (var selection in selections)
        {
            selection.Value.ApplyScaledSelection(movingData[selection.Key], lastPasteStartTick);
        }
    }

    public Dictionary<int, SortedDictionary<int, T>> PopDataInRange(int startTick, int endTick)
    {
        var subtractedData = MakeEmptyDataSet();
        foreach (var lane in lanes)
        {
            subtractedData[lane.Key] = lane.Value.PopTicksInRange(startTick, endTick);
        }
        return subtractedData;
    }

    public HashSet<int> GetTotalSelection()
    {
        HashSet<int> ticks = new();
        foreach (var selection in selections.Values)
        {
            ticks.UnionWith(selection);
        }

        return ticks;
    }

    public int GetTotalSelectionCount()
    {
        var sum = 0;
        foreach (var selection in selections.Values)
        {
            sum += selection.Count;
        }
        return sum;
    }

    public bool IsSelectionEmpty()
    {
        foreach (var selection in selections.Values)
        {
            if (selection.Count > 0) return false;
        }
        return true;
    }

    public Dictionary<int, HashSet<int>> GetTotalSelectionByLane()
    {
        Dictionary<int, HashSet<int>> ticks = new();
        foreach (var selection in selections)
        {
            ticks[selection.Key] = selection.Value.GetSelectedTicks();
        }
        return ticks;
    }

    public void ClearAllSelections()
    {
        foreach (var selection in selections.Values)
        {
            selection.Clear();
        }
    }

    public void ClearTickFromAllSelections(int tick)
    {
        foreach (var selection in selections.Values)
        {
            selection.Remove(tick);
        }
    }

    public void RemoveTickFromTotalSelection(int tick)
    {
        foreach (var selection in selections.Values)
        {
            selection.Remove(tick);
        }
    }

    public void SelectAll()
    {
        foreach (var selection in selections.Values)
        {
            selection.SelectAllInLane();
        }
        Chart.InPlaceRefresh();
    }

    public void DeleteAllTicksInSelection()
    {
        foreach (var selection in selections.Values)
        {
            selection.PopSelectedTicksFromLane();
        }
    }

    public void ShiftClickSelect(int start, int end)
    {
        foreach (var selection in selections.Values)
        {
            selection.ShiftClickSelectInRange(start, end);
        }
    }

    public Dictionary<int, SortedDictionary<int, T>> PopAllEventsAtTick(int tick)
    {
        var poppedOutput = MakeEmptyDataSet();

        foreach (var lane in lanes)
        {
            if (lane.Value.Contains(tick))
            {
                poppedOutput[lane.Key] = lane.Value.PopSingle(tick);
            }
        }

        return poppedOutput;
    }

    public Dictionary<int, SortedDictionary<int, T>> PopTickFromLane(int tick, int lane)
    {
        if (!lanes[lane].Contains(tick)) return null;

        var poppedOutput = MakeEmptyDataSet();

        var poppedTick = lanes[lane].PopSingle(tick);
        if (poppedTick == null) return null;

        poppedOutput[lane] = poppedTick;

        selections[lane].Remove(tick);

        return poppedOutput;
    }

    public void SetSelectionToNewLane(int destinationLane)
    {
        var selection = GetTotalSelectionByLane();
        var targetLane = lanes[destinationLane];
        var targetLaneSelection = selections[destinationLane];

        foreach (var lane in lanes)
        {
            if (lane.Key == destinationLane) continue;

            var laneSelection = selection[lane.Key];
            if (laneSelection.Count == 0) continue;
            
            foreach (var selectedNote in laneSelection)
            {
                targetLane[selectedNote] = lane.Value[selectedNote];
                lane.Value.Remove(selectedNote);
                targetLaneSelection.Add(selectedNote);
            }
        }
    }

    public int Count => lanes.Count;
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