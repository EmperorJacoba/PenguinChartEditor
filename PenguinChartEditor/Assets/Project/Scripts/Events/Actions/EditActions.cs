using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

// Based on https://refactoring.guru/design-patterns/command
public interface IEditAction<T> where T : IEventData
{
    public void Undo();
    public SortedDictionary<int, T> SaveData { get; set; }
}

public class Delete<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    LaneSet<T> eventSetReference;
    public Delete(LaneSet<T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    /// <summary>
    /// Delete all events specified in a selection set.
    /// </summary>
    /// <param name="selectedEvents"></param>
    /// <returns></returns>
    public bool Execute(SelectionSet<T> selection)
    {
        if (eventSetReference.Count == 0 || selection.Count == 0) return false;

        SaveData = eventSetReference.PopTicksFromSet(selection.ExportData());

        selection.Clear();
        return true;
    }

    /// <summary>
    /// Delete all events within two points in the target set.
    /// </summary>
    /// <param name="startDeleteTick"></param>
    /// <param name="endDeleteTick"></param>
    /// <returns></returns>
    public bool Execute(int startDeleteTick, int endDeleteTick)
    {
        if (eventSetReference.Count == 0) return false;

        SaveData = eventSetReference.PopTicksInRange(startDeleteTick, endDeleteTick);

        return true;
    }

    public bool Execute(int tick)
    {
        if (eventSetReference.Count == 0 || !eventSetReference.ContainsKey(tick)) return false;

        var saveDataCandidate = eventSetReference.PopSingle(tick);
        if (saveDataCandidate == null) return false; // if tried to delete immune tick

        SaveData = saveDataCandidate;

        return true;
    }

    public void Undo()
    {

    }
}

public class Create<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    LaneSet<T> eventSetReference;

    public Create(LaneSet<T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(int newTick, T newData, SelectionSet<T> selection)
    {
        // All editing of events does not come from adding an event that already exists
        // Do not create event if one already exists at that point in the set
        // If modification is required, user will drag/double click/delete etc.
        // Since creating new event in BPM/TS inherits the last event's properties,
        // Creating the same event twice is a waste of computing power.
        if (eventSetReference.ContainsKey(newTick))
        {
            selection.Clear();
            return false;
        }
        eventSetReference.Add(newTick, newData);
        SaveData.Add(newTick, newData);
        return true;
    }

    public void Undo()
    {
        foreach (var item in SaveData)
        {
            eventSetReference.Remove(item.Key);
        }
    }
}

public class Move<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    public LaneSet<T> poppedData = new();
    LaneSet<T> eventSetReference;

    public Move(LaneSet<T> targetEventSet, SortedDictionary<int, T> movingGhostSet, int offset)
    {
        eventSetReference = targetEventSet;
        SaveData = eventSetReference.ExportData();

        RemoveMovingData(movingGhostSet, offset);
    }

    // Change this so that save data still preserves original state
    // Or at least recombine this data with save data upon undo?
    void RemoveMovingData(SortedDictionary<int, T> movingGhostSet, int offset)
    {
        foreach (var tick in movingGhostSet.Keys)
        {
            SaveData.Remove(tick + offset, out T data);
            poppedData.Add(tick + offset, data);
        }
    }

    public void Undo()
    {

    }
}

public class Sustain<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();

    LaneSet<T> eventSetReference;

    public Sustain(LaneSet<T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public void CaptureOriginalSustain(List<int> ticks)
    {
        foreach (var tick in ticks)
        {
            if (eventSetReference.ContainsKey(tick))
            {
                SaveData.Add(tick, eventSetReference[tick]);
            }
        }
    }

    public void Undo()
    {

    }
}