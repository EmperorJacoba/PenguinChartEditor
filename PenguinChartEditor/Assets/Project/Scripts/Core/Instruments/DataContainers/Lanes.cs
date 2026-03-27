using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// This interface mainly exists so that SyncTrackLanes, a very special swanky little guy, can coexist with Lanes<T> 
// without needing to separate SyncTrackInstrument from all other instruments and repeating lots of code.
// SyncTrackInstrument cannot use the regular Lanes<T> object because it handles two very different data types
// (BPM & TS events) in the same context (in terms of selection, deletion, etc). 
public interface IMultiLaneController : IEnumerable<LanePairing>
{
    ILaneData GetLane(int lane);
    ISelection GetLaneSelection(int lane);
    
    bool TryGetTick(int lane, int tick, out IEventData data);
    List<int> GetUniqueTickSet();

    public int GetFirstSelectionTick();
    public MinMaxTicks GetSelectionBounds();

    public bool AnyLaneContainsTick(int tick);

    public HashSet<int> GetUnifiedSelection();
    public int GetTotalSelectionCount();
    public bool IsSelectionEmpty();
    public Dictionary<int, HashSet<int>> GetTotalSelectionByLane();
    
    void ClearAllSelections();
    void ClearTickFromAllSelections(int tick);
    void SelectAll();
    bool DeleteSelection();
    void ShiftClickSelect(int start, int end);
    void ShiftClickSelect(int start, int end, List<int> targetLanes);

    void DeleteAllEventsAtTick(int tick);
    void DeleteTickInLane(int tick, int lane);
    IEventData PopTickFromLane(int tick, int lane);

    ISelectionSnapshot TakeSelectionSnapshot();
    void ReinstateSelectionSnapshot(ISelectionSnapshot selectionSnapshot);
    public void DeleteFromSelectionSnapshot(ISelectionSnapshot selectionSnapshot);

    public Dictionary<int, IEventData> GetAllTickDataAtTick(int tick);
    public void ReinstateTickSnapshot(int tick, Dictionary<int, IEventData> tickData);

    public void ReinstateSelectionSnapshot(ISelectionSnapshot incomingSelectionData,
        ISelectionSnapshot removingSelectionData);

    ISelectionSnapshot PopTicksInRange(int startTick, int endTick);
    ISelectionSnapshot PeekTicksInRange(int startTick, int endTick);
    ISelectionSnapshot TakeNormalizedSelectionSnapshot();
    ISelectionSnapshot SnapTicks(List<int> ticks);

    public void AddTicksFromSet(ISelectionSnapshot incomingData, out ISelectionSnapshot overwrittenData);
    void SelectTicksFromSnapshot(ISelectionSnapshot newMoveData);

    bool CreateEvent(int tick, int lane, IEventData data, out AddSingleDataPackage actionInfo);
    bool IsTickInLane(int tick, int lane);
}

public struct LanePairing
{
    public int laneID;
    public ILaneData LaneData;

    public LanePairing(int laneID, ILaneData laneData)
    {
        this.laneID = laneID;
        LaneData = laneData;
    }
}

public class Lanes<T> : IMultiLaneController where T : IEventData
{
    #region Constants

    public const int NO_TICK_EVENT = -1;
    public const int INVALID_LANE = int.MinValue;
    
    #endregion
    
    #region Data
    
    public List<int> LaneKeys => lanes.Keys.ToList();
    private readonly Dictionary<int, LaneSet<T>> lanes;
    private readonly Dictionary<int, SelectionSet<T>> selections;

    public LaneSet<T> GetLane(int lane) => lanes[lane];
    ILaneData IMultiLaneController.GetLane(int lane) => lanes[lane];
    
    public SelectionSet<T> GetLaneSelection(int lane) => selections[lane];
    ISelection IMultiLaneController.GetLaneSelection(int lane) => selections[lane];
    
    bool IMultiLaneController.TryGetTick(int lane, int tick, out IEventData data)
    {
        var state = TryGetTick(lane, tick, out var typedData);
        data = (IEventData)typedData;
        return state;
    }

    public bool TryGetTick(int lane, int tick, out T data)
    {
        return lanes[lane].TryGetValue(tick, out data);
    }
    
    public int Count => lanes.Count;

    #endregion
    
    #region Constructor
    
    public Lanes(int laneCount)
    {
        lanes = new Dictionary<int, LaneSet<T>>(laneCount);
        selections = new Dictionary<int, SelectionSet<T>>(laneCount);

        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new LaneSet<T>(i);
            selections[i] = new SelectionSet<T>(lanes[i]);
        }
    }

    public Lanes(List<int> laneIDs)
    {
        lanes = new Dictionary<int, LaneSet<T>>(laneIDs.Count);
        selections = new Dictionary<int, SelectionSet<T>>(laneIDs.Count);

        foreach (var id in laneIDs)
        {
            lanes[id] = new LaneSet<T>(id);
            selections[id] = new SelectionSet<T>(lanes[id]);
        }
    }
    
    #endregion

    #region UpdateNeededEvent
    
    public delegate void UpdateNeededDelegate(int startTick, int endTick);

    /// <summary>
    /// Invoked whenever a hopo check needs to happen at a certain tick. 
    /// When invoked, the tick from the delegate should be checked to see if it or its surrounding ticks have changed hopo status.
    /// </summary>
    public event UpdateNeededDelegate UpdatesNeededInRange;
    
    #endregion
    
    #region Internal Tools

    ///<summary> Create an empty laneID:laneDataDict dictionary with no null lane data dicts. </summary>
    private Dictionary<int, SortedDictionary<int, T>> MakeEmptyDataSet()
    {
        Dictionary<int, SortedDictionary<int, T>> outputSet = new();
        foreach (var set in lanes)
        {
            outputSet[set.Key] = new SortedDictionary<int, T>();
        }
        return outputSet;
    }
    
    #endregion
    
    #region Tick Calculations
    
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

    public bool IsTickChord(int tick, out ILaneData lastFoundLane)
    {
        int noteCount = 0;
        lastFoundLane = null;
        
        foreach (var lane in lanes)
        {
            if (lane.Value.Contains(tick))
            {
                lastFoundLane = lane.Value;
                noteCount++;
            }

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
    
    public TickBounds GetTickEventBounds(int tick)
    {
        var ticks = GetUniqueTickSet();

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
        var prev = index == 0 ? NO_TICK_EVENT : ticks[index - 1];

        return new TickBounds(prev, next);
    }
    
    // FIXME:   Reduce expensive call to aggregate lane data as a sorted list by tracking tick modifications over time.
    //          Would reduce this O(nlogn) call (0.5-1ms on average) to O(1) over time 
    //          This would also require ALL add commands, delete commands, modifications to go through the lane controller.
    //          New approach would be annoying to initially implement but would likely be worth it in the long run.
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

    
    public bool AnyLaneContainsTick(int tick) => lanes.Values.Any(lane => lane.Contains(tick));

    #endregion

    #region Export

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

    public Dictionary<int, SortedDictionary<int, T>> ExportData()
    {
        var exportedData = MakeEmptyDataSet();
        foreach (var lane in lanes)
        {
            exportedData[lane.Key] = lane.Value.ExportData();
        }
        return exportedData;
    }

    public Dictionary<int, SortedDictionary<int, T>> ExportSelectionData()
    {
        var exportedData = MakeEmptyDataSet();
        foreach (var selection in selections)
        {
            exportedData[selection.Key] = selection.Value.ExportData();
        }

        return exportedData;
    }

    #endregion

    #region Selections

    public ISelectionSnapshot TakeSelectionSnapshot() => new SelectionSnapshot<T>(this);

    public ISelectionSnapshot SnapTicks(List<int> ticks)
    {
        var capturedData = new Dictionary<int, SortedDictionary<int, T>>();
        foreach (var lane in lanes)
        {
            capturedData[lane.Key] = lane.Value.SelectTicksFromSet(ticks);
        }

        return new SelectionSnapshot<T>(capturedData);
    }

    public void ReinstateSelectionSnapshot(ISelectionSnapshot selectionSnapshot)
    {
        var selectionTyped = selectionSnapshot as SelectionSnapshot<T>;

        foreach (var dataSegment in selectionTyped.savedSelectionData)
        {
            lanes[dataSegment.Key].AddTicksFromSet(dataSegment.Value);
        }
    }

    public void ReinstateSelectionSnapshot(ISelectionSnapshot incomingSelectionData,
        ISelectionSnapshot removingSelectionData)
    {
        var selectionIncTyped = incomingSelectionData as SelectionSnapshot<T>;
        var selectionRemTyped = removingSelectionData as SelectionSnapshot<T>;

        foreach (var dataSegment in selectionRemTyped.savedSelectionData)
        {
            lanes[dataSegment.Key].DeleteTicksFromSet(dataSegment.Value.Keys);

            if (selectionIncTyped.savedSelectionData.TryGetValue(dataSegment.Key, out var tickSet))
            {
                lanes[dataSegment.Key].AddTicksFromSet(tickSet);
            }
        }
    }

    public void DeleteFromSelectionSnapshot(ISelectionSnapshot selectionSnapshot)
    {
        var selectionTyped = selectionSnapshot as SelectionSnapshot<T>;

        foreach (var dataSegment in selectionTyped.savedSelectionData)
        {
            lanes[dataSegment.Key].DeleteTicksFromSet(dataSegment.Value.Keys);
        }
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
    
    public int GetTotalSelectionCount()
    {
        var sum = 0;
        foreach (var selection in selections.Values)
        {
            sum += selection.Count;
        }
        return sum;
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
    
    public Dictionary<int, HashSet<int>> GetTotalSelectionByLane()
    {
        Dictionary<int, HashSet<int>> ticks = new();
        foreach (var selection in selections)
        {
            ticks[selection.Key] = selection.Value.GetSelectedTicks();
        }
        return ticks;
    }

    public bool IsSelectionEmpty()
    {
        foreach (var selection in selections.Values)
        {
            if (selection.Count > 0) return false;
        }
        return true;
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

    public void SelectAll()
    {
        foreach (var selection in selections.Values)
        {
            selection.SelectAllInLane();
        }
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
        DeleteSelection();
        
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
    
    
    #endregion

    #region Overwrite

    public void OverwriteAllLaneData(Dictionary<int, SortedDictionary<int, T>> newData)
    { 
        foreach (var newDataLane in newData)
        {
            lanes[newDataLane.Key].OverwriteAllLaneDataWith(newDataLane.Value);
        }
    }
    
    public void OverwriteTicksFromSet(Dictionary<int, SortedDictionary<int, T>> newData)
    {
        foreach (var newDataLane in newData)
        {
            lanes[newDataLane.Key].AddTicksFromSet(newDataLane.Value);
        }
    }

    public void OverwriteLaneDataWithOffset(Dictionary<int, SortedDictionary<int, T>> newData, int offset)
    {
        MinMaxTracker tracker = new(Count);

        foreach (var newDataLane in newData.Where(newDataLane => newDataLane.Value.Count != 0))
        {
            lanes[newDataLane.Key].OverwriteDataWithOffset(newDataLane.Value, offset);
            var keys = newDataLane.Value.Keys;
            tracker.AddTickMinMax(keys.Min(), keys.Max());
        }

        var ticks = tracker.GetAbsoluteMinMax();
        UpdatesNeededInRange?.Invoke(ticks.min, ticks.max);
    }

    public void AddTicksFromSet(ISelectionSnapshot incomingData, out ISelectionSnapshot overwrittenData)
    {
        var dataTyped = incomingData as SelectionSnapshot<T>;
        AddTicksFromSet(dataTyped.savedSelectionData, out overwrittenData);
    }
    
    public void AddTicksFromSet(Dictionary<int, SortedDictionary<int, T>> newData, out ISelectionSnapshot overwrittenDataSnapshot)
    {
        MinMaxTracker tracker = new(Count);
        Dictionary<int, SortedDictionary<int, T>> overwrittenDataByLane = new();
        
        foreach (var newDataLane in newData.Where(newDataLane => newDataLane.Value.Count != 0))
        {
            lanes[newDataLane.Key].AddTicksFromSet(newDataLane.Value, out var overwrittenData);
            overwrittenDataByLane[newDataLane.Key] = overwrittenData;
            
            var keys = newDataLane.Value.Keys;
            tracker.AddTickMinMax(keys.Min(), keys.Max());
        }

        var ticks = tracker.GetAbsoluteMinMax();
        UpdatesNeededInRange?.Invoke(ticks.min, ticks.max);

        overwrittenDataSnapshot = new SelectionSnapshot<T>(overwrittenDataByLane);
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
    
    #endregion

    #region Pop/Delete
    
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

    ISelectionSnapshot IMultiLaneController.PopTicksInRange(int startTick, int endTick) =>
        new SelectionSnapshot<T>(PopTicksInRange(startTick, endTick));
    
    public Dictionary<int, SortedDictionary<int, T>> PopTicksInRange(int startTick, int endTick)
    {
        return lanes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PopTicksInRange(startTick, endTick));
    }

    ISelectionSnapshot IMultiLaneController.PeekTicksInRange(int startTick, int endTick) =>
        new SelectionSnapshot<T>(PeekTicksInRange(startTick, endTick));

    public Dictionary<int, SortedDictionary<int, T>> PeekTicksInRange(int startTick, int endTick)
    {
        return lanes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PeekTicksInRange(startTick, endTick));
    }

    public void DeleteAllEventsAtTick(int tick) => PopAllEventsAtTick(tick);

    public Dictionary<int, SortedDictionary<int, T>> PopAllEventsAtTick(int tick)
    {
        var poppedOutput = MakeEmptyDataSet();

        foreach (var lane in lanes)
        {
            if (lane.Value.Contains(tick))
            {
                lane.Value.PopSingle(tick);
            }
        }

        return poppedOutput;
    }

    public void DeleteTickInLane(int tick, int lane) => PopTickFromLane(tick, lane);
    public IEventData PopTickFromLane(int tick, int lane) => PopTickFromLaneTyped(tick, lane);
    public T PopTickFromLaneTyped(int tick, int lane)
    {
        var data = lanes[lane].PopSingleTyped(tick);
        selections[lane].Remove(tick);
        return data;
    }
    
    public bool DeleteSelection()
    {
        if (GetTotalSelectionCount() == 0) return false;
        foreach (var selection in selections.Values)
        {
            selection.PopSelectedTicksFromLane();
        }
        return true;
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
            var removableData = Enumerable.ToHashSet(lane.Value.Where<KeyValuePair<int, T>>
            (
                kvp =>
                    kvp.Key >= startTick &&
                    kvp.Key <= endTick &&
                    !selections[lane.Key].Contains(kvp.Key)
            ));
                
            foreach (var @event in removableData)
            {
                lane.Value.Remove(@event.Key);
                selections[lane.Key].Remove(@event.Key);
            }
        }
    }
    
    #endregion

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
    
    public Dictionary<int, IEventData> GetAllTickDataAtTick(int tick)
    {
        var dictionary = new Dictionary<int, IEventData>();

        foreach (var lane in lanes)
        {
            if (lane.Value.TryGetValue(tick, out IEventData data))
            {
                dictionary[lane.Key] = data;
            }
        }

        return dictionary;
    }

    public void ReinstateTickSnapshot(int tick, Dictionary<int, IEventData> tickData)
    {
        foreach (var lane in tickData)
        {
            lanes[lane.Key].CreateEvent(tick, lane.Value, out _);
        }
    }

    public void DebugPrintSelectionCount()
    {
        var output = selections.Where(selection => selection.Value.Count != 0).Aggregate("", (current, selection) => current + $"{selection.Key}: {selection.Value.Count}");
        MonoBehaviour.print(output);
    }

    public ISelectionSnapshot TakeNormalizedSelectionSnapshot()
    {
        var selectionNormalized = ExportNormalizedSelection();
        return new SelectionSnapshot<T>(selectionNormalized);
    }
    
    public void SelectTicksFromSnapshot(ISelectionSnapshot newMoveData)
    {
        var selectionTyped = newMoveData as SelectionSnapshot<T>;
        var data = selectionTyped.savedSelectionData;

        foreach (var lane in data)
        {
            var laneID = lane.Key;
            var laneData = lane.Value;
            
            selections[laneID].SelectTicksFromSet(laneData);
        }
    }

    public bool CreateEvent(int tick, int lane, IEventData data, out AddSingleDataPackage actionInfo)
    {
        return lanes[lane].CreateEvent(tick, data, out actionInfo);
    }

    public bool IsTickInLane(int tick, int lane)
    {
        return lanes[lane].Contains(tick);
    }

    public IEnumerator<LanePairing> GetEnumerator() => lanes.Select(lane => new LanePairing(lane.Key, lane.Value)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <remarks>Access TempoEvents with Lane = 0. Access TimeSignatureEvents with Lane = 1.</remarks>
public class SyncTrackLanes : IMultiLaneController
{
    // Data types are fundamentally different, very hard to combine into one single Lanes object
    // Both are also structs because of their small (and repeatable) size.
    // Converting from IEventData to XData every time you want to get a statistic would be too much overhead. 
    public LaneSet<BPMData> TempoEvents { get; } // Lane = 0
    public LaneSet<TSData> TimeSignatureEvents { get; } // Lane = 1

    public SelectionSet<BPMData> bpmSelection;
    public SelectionSet<TSData> tsSelection;

    public SyncTrackLanes()
    {
        TempoEvents = new LaneSet<BPMData>(
            0,
            protectedTicks: new HashSet<int> { 0 }
        );
        
        TimeSignatureEvents = new LaneSet<TSData>(
            1,
            protectedTicks: new HashSet<int> { 0 }
        );

        bpmSelection = new SelectionSet<BPMData>(TempoEvents);
        tsSelection = new SelectionSet<TSData>(TimeSignatureEvents);
    }
    
    public ILaneData GetLane(int lane) => lane == 0 ? TempoEvents : TimeSignatureEvents;
    public ISelection GetLaneSelection(int lane) => lane == 0 ? bpmSelection : tsSelection;

    public bool TryGetTick(int lane, int tick, out IEventData data) => GetLane(lane).TryGetValue(tick, out data);
    public bool IsTickInLane(int tick, int lane) => GetLane(lane).Contains(tick);

    public List<int> GetUniqueTickSet()
    {
        var hashSet = Enumerable.ToHashSet(TempoEvents.ExportData().Keys);
        hashSet.UnionWith(Enumerable.ToHashSet(TimeSignatureEvents.ExportData().Keys));
        
        List<int> list = new(hashSet);
        list.Sort();
        
        return list;
    }

    public int GetFirstSelectionTick()
    {
        HashSet<int> minSelectionTicks = new();
        
        if (bpmSelection.Count > 0) minSelectionTicks.Add(bpmSelection.Min());
        if (tsSelection.Count > 0) minSelectionTicks.Add(tsSelection.Min());
        
        return minSelectionTicks.Count > 0 ? minSelectionTicks.Min() : SelectionSet<BPMData>.NONE_SELECTED;
    }

    public MinMaxTicks GetSelectionBounds()
    {
        MinMaxTracker tracker = new(2);
        
        if (bpmSelection.Count > 0) tracker.AddTickMinMax(bpmSelection.Min(), bpmSelection.Max());
        if (tsSelection.Count > 0) tracker.AddTickMinMax(tsSelection.Min(), bpmSelection.Max());

        return tracker.GetAbsoluteMinMax();
    }

    public bool AnyLaneContainsTick(int tick) => TempoEvents.Contains(tick) || TimeSignatureEvents.Contains(tick);
    
    public HashSet<int> GetUnifiedSelection()
    {
        HashSet<int> ticks = new();
        ticks.UnionWith(bpmSelection);
        ticks.UnionWith(tsSelection);
        return ticks;
    }

    public int GetTotalSelectionCount() => bpmSelection.Count + tsSelection.Count;
    public bool IsSelectionEmpty() => GetTotalSelectionCount() == 0;

    public Dictionary<int, HashSet<int>> GetTotalSelectionByLane()
    {
        return new Dictionary<int, HashSet<int>>()
        {
            {0, bpmSelection.GetSelectedTicks()},
            {1, tsSelection.GetSelectedTicks()}
        };
    }

    public void ClearAllSelections()
    {
        bpmSelection.Clear();
        tsSelection.Clear();
    }

    public void ClearTickFromAllSelections(int tick)
    {
        bpmSelection.Remove(tick);
        tsSelection.Remove(tick);
    }

    public void SelectAll()
    {
        bpmSelection.SelectAllInLane();
        tsSelection.SelectAllInLane();
    }

    public void DeleteAllEventsAtTick(int tick)
    {
        if (TempoEvents.Contains(tick)) TempoEvents.PopSingle(tick);
        if (TimeSignatureEvents.Contains(tick)) TimeSignatureEvents.PopSingle(tick);

        Chart.SyncTrackInPlaceRefresh();
    }

    public void DeleteTickInLane(int tick, int lane) => PopTickFromLane(tick, lane);

    public IEventData PopTickFromLane(int tick, int lane)
    {
        IEventData data = default;
        switch (lane)
        {
            case 0:
            {
                if (TempoEvents.protectedTicks.Contains(tick)) return null;
                
                data = TempoEvents.PopSingle(tick);

                Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();
                Chart.SyncTrackInPlaceRefresh();
                
                break;
            }
            case 1:
            {
                if (TimeSignatureEvents.protectedTicks.Contains(tick)) return null;
                
                data = TimeSignatureEvents.PopSingle(tick);
                break;
            }
        }
        
        return data;
    }
    
    ISelectionSnapshot IMultiLaneController.PopTicksInRange(int startTick, int endTick) =>
        new SyncTrackSelectionSnapshot(
            TempoEvents.PopTicksInRange(startTick, endTick), 
            TimeSignatureEvents.PopTicksInRange(startTick, endTick)
            );
    
    ISelectionSnapshot IMultiLaneController.PeekTicksInRange(int startTick, int endTick) =>
        new SyncTrackSelectionSnapshot(
            TempoEvents.PeekTicksInRange(startTick, endTick), 
            TimeSignatureEvents.PeekTicksInRange(startTick, endTick)
            );
    

    public ISelectionSnapshot TakeSelectionSnapshot() => new SyncTrackSelectionSnapshot(this);
    
    public void ReinstateSelectionSnapshot(ISelectionSnapshot selectionSnapshot)
    {
        var selectionTyped = selectionSnapshot as SyncTrackSelectionSnapshot;

        foreach (var bpm in selectionTyped.bpmSelection)
        {
            TempoEvents[bpm.Key] = bpm.Value;
        }

        foreach (var ts in selectionTyped.tsSelection)
        {
            TimeSignatureEvents[ts.Key] = ts.Value;
        }
        
        Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();
    }

    public void ReinstateSelectionSnapshot(ISelectionSnapshot incomingSelectionData,
        ISelectionSnapshot removingSelectionData)
    {
        var typedIncData = incomingSelectionData as SyncTrackSelectionSnapshot;
        var typedRemData = removingSelectionData as SyncTrackSelectionSnapshot;

        TempoEvents.DeleteTicksFromSet(typedRemData.bpmSelection.Keys);
        TimeSignatureEvents.DeleteTicksFromSet(typedRemData.tsSelection.Keys);
        
        TempoEvents.AddTicksFromSet(typedIncData.bpmSelection);
        TimeSignatureEvents.AddTicksFromSet(typedIncData.tsSelection);
        
        Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();
    }

    public void DeleteFromSelectionSnapshot(ISelectionSnapshot selectionSnapshot)
    {
        var selectionTyped = selectionSnapshot as SyncTrackSelectionSnapshot;
        
        TempoEvents.DeleteTicksFromSet(selectionTyped.bpmSelection.Keys);
        TimeSignatureEvents.DeleteTicksFromSet(selectionTyped.tsSelection.Keys);
        
        Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();
    }

    public bool DeleteSelection()
    {
        if (IsSelectionEmpty()) return false;
        if (bpmSelection.Count > 0)
        {
            bpmSelection.PopSelectedTicksFromLane();
            Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();        
        }
        tsSelection.PopSelectedTicksFromLane();
        
        // The base instrument also does a refresh...means this is a little less efficient. But oh well, what can you do.
        // We need the special refresh, unfortunately. If you can find a not clunky solution for this issue please do.
        Chart.SyncTrackInPlaceRefresh();
        return true;
    }

    public void ShiftClickSelect(int start, int end)
    {
        bpmSelection.ShiftClickSelectInRange(start, end);
        tsSelection.ShiftClickSelectInRange(start, end);
    }

    public void ShiftClickSelect(int start, int end, List<int> targetLanes)
    {
        if (targetLanes.Contains(0)) bpmSelection.ShiftClickSelectInRange(start, end);
        if (targetLanes.Contains(1)) tsSelection.ShiftClickSelectInRange(start, end);
    }

    public Dictionary<int, IEventData> GetAllTickDataAtTick(int tick)
    {
        var dictionary = new Dictionary<int, IEventData>();
        
        if (TempoEvents.TryGetValue(tick, out IEventData bpmData))
        {
            dictionary[0] = bpmData;
        }

        if (TimeSignatureEvents.TryGetValue(tick, out IEventData tsData))
        {
            dictionary[1] = tsData;
        }

        return dictionary;
    }

    public void ReinstateTickSnapshot(int tick, Dictionary<int, IEventData> tickData)
    {
        if (tickData.TryGetValue(0, out var data))
        {
            TempoEvents.CreateEvent(tick, data, out _);
            Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();
        }

        if (tickData.TryGetValue(1, out var tsData))
        {
            TimeSignatureEvents.CreateEvent(tick, tsData, out _);
        }
    }

    public ISelectionSnapshot TakeNormalizedSelectionSnapshot()
    {
        return new SyncTrackSelectionSnapshot(bpmSelection.ExportNormalizedDataWithoutProtectedTicks(),
            tsSelection.ExportNormalizedDataWithoutProtectedTicks());
    }

    public ISelectionSnapshot SnapTicks(List<int> ticks)
    {
        return new SyncTrackSelectionSnapshot(TempoEvents.SelectTicksFromSet(ticks),
            TimeSignatureEvents.SelectTicksFromSet(ticks));
    }

    public void AddTicksFromSet(ISelectionSnapshot incomingData, out ISelectionSnapshot overwrittenData)
    {
        var selectionSnapshotTyped = incomingData as SyncTrackSelectionSnapshot;
        
        TempoEvents.AddTicksFromSet(selectionSnapshotTyped.bpmSelection, out var bData);
        TimeSignatureEvents.AddTicksFromSet(selectionSnapshotTyped.tsSelection, out var tData);

        overwrittenData = new SyncTrackSelectionSnapshot(bData, tData);
    }

    public void SelectTicksFromSnapshot(ISelectionSnapshot newMoveData)
    {
        var selectionSnapshotTyped = newMoveData as SyncTrackSelectionSnapshot;
        
        bpmSelection.SelectTicksFromSet(selectionSnapshotTyped.bpmSelection);
        tsSelection.SelectTicksFromSet(selectionSnapshotTyped.tsSelection);
    }

    public bool CreateEvent(int tick, int lane, IEventData data, out AddSingleDataPackage actionInfo)
    {
        var createdNewEvent = GetLane(lane).CreateEvent(tick, data, out actionInfo);
        if (createdNewEvent && lane == (int)SyncTrackInstrument.LaneOrientation.bpm)
        {
            Chart.SyncTrackInstrument.RecalculateTempoEventDictionary();
            Chart.SyncTrackInPlaceRefresh();
        }

        return createdNewEvent;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<LanePairing> GetEnumerator()
    {
        yield return new LanePairing((int)SyncTrackInstrument.LaneOrientation.bpm, TempoEvents);
        yield return new LanePairing((int)SyncTrackInstrument.LaneOrientation.timeSignature, TimeSignatureEvents);
    }
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