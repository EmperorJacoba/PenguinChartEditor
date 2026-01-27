using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Lanes<T> where T : IEventData
{
    public const int NO_TICK_EVENT = -1;
    public List<int> LaneKeys => lanes.Keys.ToList();
    private Dictionary<int, LaneSet<T>> lanes;
    private Dictionary<int, SelectionSet<T>> selections;
    public HashSet<int> TempSustainTicks = new();

    public Lanes(int laneCount)
    {
        lanes = new Dictionary<int, LaneSet<T>>(laneCount);
        selections = new Dictionary<int, SelectionSet<T>>(laneCount);

        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new LaneSet<T>();
            selections[i] = new SelectionSet<T>(lanes[i]);
        }
    }

    public Lanes(List<int> laneIDs)
    {
        lanes = new Dictionary<int, LaneSet<T>>(laneIDs.Count);
        selections = new Dictionary<int, SelectionSet<T>>(laneIDs.Count);

        foreach (var id in laneIDs)
        {
            lanes[id] = new LaneSet<T>();
            selections[id] = new SelectionSet<T>(lanes[id]);
        }
    }

    public delegate void UpdateNeededDelegate(int startTick, int endTick);

    /// <summary>
    /// Invoked whenever a hopo check needs to happen at a certain tick. 
    /// When invoked, the tick from the delegate should be checked to see if it or its surrounding ticks have changed hopo status.
    /// </summary>
    public event UpdateNeededDelegate UpdatesNeededInRange;

    private Dictionary<int, SortedDictionary<int, T>> MakeEmptyDataSet()
    {
        Dictionary<int, SortedDictionary<int, T>> outputSet = new();
        foreach (var set in lanes)
        {
            outputSet[set.Key] = new SortedDictionary<int, T>();
        }
        return outputSet;
    }

    public LaneSet<T> GetLane(int lane) => lanes[lane];

    public bool TryGetTick(int lane, int tick, out T data)
    {
        return lanes[lane].TryGetValue(tick, out data);
    }
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
    public void PopTicksInRange(MinMaxTicks minMaxTicks) =>
        PopTicksInRange(minMaxTicks.min, minMaxTicks.max);

    public void PopTicksInRange(int tick, ISustainable sustainedData) =>
        PopTicksInRange(tick, tick + sustainedData.Sustain);

    public void PopTicksInRange(int tick, T data)
    {
        if (data is ISustainable sustainableData)
        {
            PopTicksInRange(tick, sustainableData);
        }
        else PopTicksInRange(tick, tick);
    }
    
    public Dictionary<int, SortedDictionary<int, T>> PopTicksInRange(int startTick, int endTick)
    {
        var subtractedData = MakeEmptyDataSet();
        foreach (var lane in lanes)
        {
            subtractedData[lane.Key] = lane.Value.PopTicksInRange(startTick, endTick);
        }
        return subtractedData;
    }

    public HashSet<int> GetUnifiedSelection()
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

    public bool DeleteAllTicksInSelection()
    {
        if (GetTotalSelectionCount() == 0) return false;
        foreach (var selection in selections.Values)
        {
            selection.PopSelectedTicksFromLane();
        }
        return true;
    }

    public void ShiftClickSelect(int start, int end)
    {
        foreach (var selection in selections.Values)
        {
            selection.ShiftClickSelectInRange(start, end);
        }
    }

    public void ShiftClickSelect(int start, int end, List<int> targetLanes)
    {
        foreach (var laneID in targetLanes)
        {
            selections[laneID].ShiftClickSelectInRange(start, end);
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

    public void CopyDataToAllLanes(int lane, int tick)
    {
        if (!TryGetTick(lane, tick, out var data))
        {
            return;
        }

        PopTicksInRange(tick, data);
        
        foreach (var copiedToLane in lanes)
        {
            copiedToLane.Value.Add(tick, data);
            selections[copiedToLane.Key].Add(tick);
        }
    }

    public void DeleteAllEventsInTickDataRangeNotSelected(int lane, int tick)
    {
        if (!TryGetTick(lane, tick, out var data)) return;
        
        if (data is ISustainable sustainableData)
        {
            DeleteAllEventsInTickRangeNotSelected(tick, tick + sustainableData.Sustain);
        }
        
        DeleteAllEventsInTickRangeNotSelected(tick, tick);
    }
    
    public void DeleteAllEventsInTickRangeNotSelected(int startTick, int endTick)
    {
        foreach (var lane in lanes)
        {
            var removableData = lane.Value.Where
            (
                kvp =>
                    kvp.Key >= startTick &&
                    kvp.Key <= endTick &&
                    !selections[lane.Key].Contains(kvp.Key)
            ).ToHashSet();
                
            foreach (var @event in removableData)
            {
                lane.Value.Remove(@event.Key);
                selections[lane.Key].Remove(@event.Key);
            }
        }
    }

    public void DebugPrintSelectionCount()
    {
        var output = selections.Where(selection => selection.Value.Count != 0).Aggregate("", (current, selection) => current + $"{selection.Key}: {selection.Value.Count}");
        MonoBehaviour.print(output);
    }

    public SortedDictionary<int, T> GetUnifiedSelectionWithData()
    {
        var outputDict = new SortedDictionary<int, T>();

        foreach (var selection in selections.Values)
        {
            var selectionData = selection.ExportData();
            foreach (var data in selectionData)
            {
                outputDict[data.Key] = data.Value;
            }
        }

        return outputDict;
    }

    public SortedDictionary<int, T> CutUnifiedSelectionWithData()
    {
        var selection = GetUnifiedSelectionWithData();
        DeleteAllTicksInSelection();
        
        return selection;
    }

    public void CopySelectionToLane(int targetLane) =>
        CopySelectionToLane(targetLane, GetUnifiedSelectionWithData());
    public void CopySelectionToLane(int targetLane, SortedDictionary<int, T> selectionData)
    {
        if (selectionData.Count == 0) return;
        
        var clearZone = new MinMaxTicks(selectionData.Keys.Min(), selectionData.Keys.Max());
        var targetLaneSet = lanes[targetLane];
        targetLaneSet.PopTicksInRange(clearZone);

        foreach (var data in selectionData)
        {
            targetLaneSet[data.Key] = data.Value;
            selections[targetLane].Add(data.Key);
        }
    }

    public void MoveSelectionToLane(int targetLane)
    {
        CopySelectionToLane(targetLane, CutUnifiedSelectionWithData());
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