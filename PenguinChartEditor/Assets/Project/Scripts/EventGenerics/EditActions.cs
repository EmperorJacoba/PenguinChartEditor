using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

// Based on https://refactoring.guru/design-patterns/command
public interface IEditAction<T>
{
    public void Undo();
    public SortedDictionary<int, T> SaveData { get; set; }
}

public class Copy<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    public SortedDictionary<int, T> eventSetReference;
    public Copy(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }
    public bool Execute(SortedDictionary<int, T> clipboard, SortedDictionary<int, T> selection)
    {
        // Note: to make up for separate selection sets and clipboards,
        // make a hashset<int> property that when referenced, combines
        // all other selection sets into one so that the clipboard accurately
        // reflects the relative displacement of events on the clipboard
        // so clipboard will not always start at zero for each event

        clipboard.Clear(); // prep dictionary for new copy data

        // copy data is shifted to zero for relative pasting 
        // (ex. an event sequence 100, 200, 300 is converted to 0, 100, 200)
        int lowestTick = 0;
        if (selection.Count > 0) lowestTick = selection.Keys.Min();

        // add relevant data for each tick into clipboard
        foreach (var selectedTick in selection)
        {
            try
            {
                clipboard.Add(selectedTick.Key - lowestTick, selection[selectedTick.Key]);
            }
            catch
            {
                continue;
            }
        }
        return false; // method is NOT undoable
    }

    public void Undo()
    {
        return;
    }
}

public class Paste<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;
    public Delete<T> deleteAction;

    public Paste(SortedDictionary<int, T> targetEventSet, bool tick0Immune)
    {
        eventSetReference = targetEventSet;
        deleteAction = new(targetEventSet, tick0Immune);
    }

    public bool Execute(int startPasteTick, SortedDictionary<int, T> clipboard)
    {
        if (clipboard.Count == 0) return false;
    
        SaveData = new(eventSetReference);

        // Create a temp dictionary without events within the size of the clipboard from the origin of the paste 
        // (ex. clipboard with 0, 100, 400 has a zone of 400, paste starts at tick 700, all events tick 700-1100 are wiped)
        var endPasteTick = clipboard.Keys.Max() + startPasteTick;

        deleteAction.Execute(startPasteTick, endPasteTick);

        // Add clipboard data to dict, now cleaned of obstructing events
        foreach (var clippedTick in clipboard)
        {
            eventSetReference.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
        }

        return true;
    }

    public void Undo()
    {
        eventSetReference.Clear();
        foreach (var tick in SaveData)
        {
            eventSetReference.Add(tick.Key, tick.Value);
        }
    }
}

public class Delete<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;
    public Delete(SortedDictionary<int, T> targetEventSet, bool tick0Immune)
    {
        eventSetReference = targetEventSet;
        this.tick0Immune = tick0Immune;
    }
    int startTick;
    int endTick;
    bool tick0Immune;

    /// <summary>
    /// Delete all events specified in a selection set.
    /// </summary>
    /// <param name="selectedEvents"></param>
    /// <returns></returns>
    public bool Execute(SortedDictionary<int, T> selectedEvents)
    {
        if (eventSetReference.Count == 0 || selectedEvents.Count == 0) return false;

        foreach (var tick in selectedEvents)
        {
            if (tick.Key == 0 && tick0Immune) continue;
            if (eventSetReference.ContainsKey(tick.Key))
            {
                eventSetReference.Remove(tick.Key, out T data);
                SaveData.Add(tick.Key, data);
            }
        }

        selectedEvents.Clear();
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

        var overwritableEvents = GetOverwritableDictEvents(eventSetReference, startDeleteTick, endDeleteTick);
        if (overwritableEvents.Count == 0) return false;

        foreach (var tick in overwritableEvents)
        {
            eventSetReference.Remove(tick, out T data);
            SaveData.Add(tick, data);
        }

        startTick = startDeleteTick;
        endTick = endDeleteTick;
        return true;
    }

    public bool Execute(int tick)
    {
        if (eventSetReference.Count == 0 || !eventSetReference.ContainsKey(tick)) return false;
        if (tick0Immune && tick == 0) return false;

        eventSetReference.Remove(tick, out T data);
        SaveData.Add(tick, data);

        startTick = tick;
        endTick = tick;
        return true;
    }

    public void Undo()
    {
        var ticksToClear = GetOverwritableDictEvents(eventSetReference, startTick, endTick);

        foreach (var tick in ticksToClear)
        {
            eventSetReference.Remove(tick);
        }

        foreach (var tick in SaveData)
        {
            eventSetReference.Add(tick.Key, tick.Value);
        }
    }
    
    HashSet<int> GetOverwritableDictEvents(SortedDictionary<int, T> eventSet, int startPasteTick, int endPasteTick)
    {
        return eventSet.Keys.ToList().Where(x => x >= startPasteTick && x <= endPasteTick).ToHashSet();
    }
}

public class Cut<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;
    Delete<T> deleteAction;
    public Cut(SortedDictionary<int, T> targetEventSet, bool tick0Immune)
    {
        eventSetReference = targetEventSet;
        deleteAction = new(eventSetReference, tick0Immune);
    }

    public bool Execute(SortedDictionary<int, T> clipboard, SortedDictionary<int, T> selection)
    {
        var copyAction = new Copy<T>(eventSetReference);
        copyAction.Execute(clipboard, selection);

        if (deleteAction.Execute(selection)) return true;

        return false;
    }

    public void Undo()
    {
        deleteAction.Undo();
    }
}

public class Create<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;

    public Create(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(int newTick, T newData, SortedDictionary<int, T> selectedEvents)
    {
        // All editing of events does not come from adding an event that already exists
        // Do not create event if one already exists at that point in the set
        // If modification is required, user will drag/double click/delete etc.
        // Since creating new event in BPM/TS inherits the last event's properties,
        // Creating the same event twice is a waste of computing power.
        if (eventSetReference.ContainsKey(newTick))
        {
            selectedEvents.Clear();
            return false;
        }

        eventSetReference.Remove(newTick);
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

public class Move<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    public SortedDictionary<int, T> poppedData = new();
    SortedDictionary<int, T> eventSetReference;

    public Move(SortedDictionary<int, T> targetEventSet, SortedDictionary<int, T> movingGhostSet, int offset)
    {
        eventSetReference = targetEventSet;
        SaveData = new(eventSetReference);

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

public class Sustain<T> : IEditAction<T>
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();

    SortedDictionary<int, T> eventSetReference;

    public Sustain(SortedDictionary<int, T> targetEventSet)
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