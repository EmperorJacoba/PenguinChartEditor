using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

// Based on https://refactoring.guru/design-patterns/command
public interface IEditAction<DataType>
{
    public void Undo();
    public SortedDictionary<int, DataType> SaveData { get; set; }
}

public class Copy<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; } = new();
    public SortedDictionary<int, DataType> eventSetReference;
    public Copy(SortedDictionary<int, DataType> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }
    public bool Execute(SortedDictionary<int, DataType> clipboard, HashSet<int> selection)
    {
        clipboard.Clear(); // prep dictionary for new copy data

        // copy data is shifted to zero for relative pasting 
        // (ex. an event sequence 100, 200, 300 is converted to 0, 100, 200)
        int lowestTick = 0;
        if (selection.Count > 0) lowestTick = selection.Min();

        // add relevant data for each tick into clipboard
        foreach (var selectedTick in selection)
        {
            try
            {
                clipboard.Add(selectedTick - lowestTick, eventSetReference[selectedTick]);
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

public class Paste<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; } = new();
    SortedDictionary<int, DataType> eventSetReference;

    public Paste(SortedDictionary<int, DataType> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(int startPasteTick, SortedDictionary<int, DataType> clipboard)
    {
        if (clipboard.Count > 0) // avoid index error
        {
            SaveData = new(eventSetReference);

            // Create a temp dictionary without events within the size of the clipboard from the origin of the paste 
            // (ex. clipboard with 0, 100, 400 has a zone of 400, paste starts at tick 700, all events tick 700-1100 are wiped)
            var endPasteTick = clipboard.Keys.Max() + startPasteTick;

            // get overwritable dictionary events
            // remove those events
            // add new events
            var overwritableEvents = GetOverwritableDictEvents(eventSetReference, startPasteTick, endPasteTick);
            foreach (var tick in overwritableEvents)
            {
                eventSetReference.Remove(tick);
            }

            // Add clipboard data to dict, now cleaned of obstructing events
            foreach (var clippedTick in clipboard)
            {
                eventSetReference.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
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

    HashSet<int> GetOverwritableDictEvents(SortedDictionary<int, DataType> eventSet, int startPasteTick, int endPasteTick)
    {
        return eventSet.Keys.ToList().Where(x => x > startPasteTick && x < endPasteTick).ToHashSet();
    }

}

public class Delete<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; } = new();
    SortedDictionary<int, DataType> eventSetReference;
    public Delete(SortedDictionary<int, DataType> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(HashSet<int> selectedEvents)
    {
        if (eventSetReference.Count != 0)
        {
            foreach (var tick in selectedEvents)
            {
                if (tick != 0)
                {
                    DataType data;
                    eventSetReference.Remove(tick, out data);
                    SaveData.Add(tick, data);
                }
            }
        }
        selectedEvents.Clear();
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

public class Cut<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; } = new();
    SortedDictionary<int, DataType> eventSetReference;
    Delete<DataType> deleteAction;
    public Cut(SortedDictionary<int, DataType> targetEventSet)
    {
        eventSetReference = targetEventSet;
        deleteAction = new(eventSetReference);
    }

    public bool Execute(SortedDictionary<int, DataType> clipboard, HashSet<int> selection)
    {
        var copyAction = new Copy<DataType>(eventSetReference);
        copyAction.Execute(clipboard, selection);

        deleteAction.Execute(selection);

        return true;
    }

    public void Undo()
    {
        deleteAction.Undo();
    }
}

public class Create<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; } = new();
    SortedDictionary<int, DataType> eventSetReference;

    public Create(SortedDictionary<int, DataType> targetEventSet)
    {
        eventSetReference = targetEventSet;
    }

    public bool Execute(int newTick, DataType newData, HashSet<int> selectedEvents)
    {
        if (eventSetReference.ContainsKey(newTick))
        {
            selectedEvents.Clear();
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
// Each command has data that explains how to undo itself
// When command is executed, add that command to an array of commands, and have a function that calls the undo on whatever command is next in the array of commands when undo is clicked
// 

// BPM: for every action that inherits from IEditAction, fire the recalculate command?