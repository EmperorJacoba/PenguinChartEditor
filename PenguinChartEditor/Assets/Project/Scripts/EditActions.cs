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

public class Copy<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    public SortedDictionary<int, T> eventSetReference;
    public Copy(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }
    public bool Execute()
    {
        SelectionManager.clipboard.Clear(); // prep dictionary for new copy data

        // copy data is shifted to zero for relative pasting 
        // (ex. an event sequence 100, 200, 300 is converted to 0, 100, 200)
        int lowestTick = 0;
        if (SelectionManager.selection.Count > 0) lowestTick = SelectionManager.selection.Keys.Min();

        // add relevant data for each tick into clipboard
        foreach (var selectedTick in SelectionManager.selection)
        {
            if (SelectionManager.clipboard.ContainsKey(selectedTick.Key))
            {
                SelectionManager.clipboard[selectedTick.Key - lowestTick].Add(eventSetReference[selectedTick.Key]);
            }
            else
            {
                SelectionManager.clipboard.Add(selectedTick.Key - lowestTick, new() { eventSetReference[selectedTick.Key] });
            }
        }
        return false; // method is NOT undoable
    }

    public void Undo()
    {
        return;
    }
}

public class Paste<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;

    public Paste(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(int startPasteTick)
    {
        if (SelectionManager.clipboard.Count > 0) // avoid index error
        {
            SaveData = new(eventSetReference);

            // Create a temp dictionary without events within the size of the clipboard from the origin of the paste 
            // (ex. clipboard with 0, 100, 400 has a zone of 400, paste starts at tick 700, all events tick 700-1100 are wiped)
            var endPasteTick = SelectionManager.clipboard.Keys.Max() + startPasteTick;

            // get overwritable dictionary events
            // remove those events
            // add new events
            var overwritableEvents = GetOverwritableDictEvents(eventSetReference, startPasteTick, endPasteTick);
            foreach (var tick in overwritableEvents)
            {
                eventSetReference.Remove(tick);
            }

            // Add clipboard data to dict, now cleaned of obstructing events
            foreach (var clippedTick in SelectionManager.clipboard)
            {
                if (SelectionManager.clipboard[clippedTick.Key].OfType<T>().Any())
                    eventSetReference.Add(clippedTick.Key + startPasteTick, SelectionManager.clipboard[clippedTick.Key].OfType<T>().FirstOrDefault());
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public void Undo()
    {
        eventSetReference.Clear();
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

public class Delete<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;
    public Delete(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute()
    {
        if (eventSetReference.Count != 0)
        {
            foreach (var tick in SelectionManager.selection)
            {
                if (tick.Key != 0 && eventSetReference.ContainsKey(tick.Key))
                {
                    eventSetReference.Remove(tick.Key, out T data);
                    SaveData.Add(tick.Key, data);
                }
            }
        }
        SelectionManager.selection.Clear();
        return true;
    }

    public bool Execute(HashSet<int> targetTicks)
    {
        if (eventSetReference.Count != 0)
        {
            foreach (var tick in targetTicks)
            {
                if (tick != 0 && eventSetReference.ContainsKey(tick))
                {
                    eventSetReference.Remove(tick, out T data);
                    SaveData.Add(tick, data);
                }
            }
        }
        return true;
    }

    public void Undo()
    {
        foreach (var tick in SaveData)
        {
            eventSetReference.Add(tick.Key, tick.Value);
        }
    }
}

public class Cut<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;
    Delete<T> deleteAction;
    public Cut(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
        deleteAction = new(eventSetReference);
    }

    public bool Execute()
    {
        var copyAction = new Copy<T>(eventSetReference);
        copyAction.Execute();

        deleteAction.Execute();

        return true;
    }

    public void Undo()
    {
        deleteAction.Undo();
    }
}

public class Create<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;

    public Create(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(int newTick, T newData)
    {
        if (!eventSetReference.ContainsKey(newTick))
        {
            SelectionManager.selection.Clear();
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

public class Move<T> : IEditAction<T> where T : IEventData
{
    public SortedDictionary<int, T> SaveData { get; set; } = new();
    SortedDictionary<int, T> eventSetReference;
    Create<T> createAction;
    Delete<T> deleteAction;

    public Move(SortedDictionary<int, T> targetEventSet)
    {
        eventSetReference = targetEventSet;
        createAction = new(targetEventSet);
        deleteAction = new(targetEventSet);
    }

    public bool Execute(int targetTick, int destinationTick, HashSet<int> selectedEvents)
    {
        var copiedData = eventSetReference[targetTick];
        if (eventSetReference.ContainsKey(destinationTick)) SaveData.Add(destinationTick, eventSetReference[destinationTick]);
        HashSet<int> ticksToWipe = new()
        {
            targetTick,
            destinationTick
        };

        deleteAction.Execute(ticksToWipe);
        createAction.Execute(destinationTick, copiedData);

        return true;
    }

    public void Undo()
    {
        createAction.Undo();
        foreach (var tick in SaveData) // undo overwrite in case overwrite occurs
        {
            eventSetReference.Add(tick.Key, tick.Value);
        }
        deleteAction.Undo();
    }
}
// need better solution over passing in a selected events set...also need master selected events dictionary
    // BPM/TS structs inherit from IEventData interface for value tagging?
// Each command has data that explains how to undo itself
// When command is executed, add that command to an array of commands, and have a function that calls the undo on whatever command is next in the array of commands when undo is clicked
// 

// BPM: for every action that inherits from IEditAction, fire the recalculate command?