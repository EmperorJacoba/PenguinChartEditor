using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Based on https://refactoring.guru/design-patterns/command
public interface IEditAction<DataType>
{
    public void Undo();
    public SortedDictionary<int, DataType> SaveData { get; set; }
}

public class Copy<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; }
    public bool Execute(SortedDictionary<int, DataType> clipboard, HashSet<int> selection, SortedDictionary<int, DataType> targetEventSet)
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
                clipboard.Add(selectedTick - lowestTick, targetEventSet[selectedTick]);
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
    public SortedDictionary<int, DataType> SaveData { get; set; }
    SortedDictionary<int, DataType> eventSetReference;

    public bool Execute(int startPasteTick, SortedDictionary<int, DataType> clipboard, SortedDictionary<int, DataType> targetEventSet)
    {
        if (clipboard.Count > 0) // avoid index error
        {
            SaveData = new(targetEventSet);
            eventSetReference = targetEventSet;

            // Create a temp dictionary without events within the size of the clipboard from the origin of the paste 
            // (ex. clipboard with 0, 100, 400 has a zone of 400, paste starts at tick 700, all events tick 700-1100 are wiped)
            var endPasteTick = clipboard.Keys.Max() + startPasteTick;

            // get overwritable dictionary events
            // remove those events
            // add new events
            var overwritableEvents = GetOverwritableDictEvents(targetEventSet, startPasteTick, endPasteTick);
            foreach (var tick in overwritableEvents)
            {
                targetEventSet.Remove(tick);
            }

            // Add clipboard data to dict, now cleaned of obstructing events
            foreach (var clippedTick in clipboard)
            {
                targetEventSet.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
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
        return eventSet.Keys.Where(x => x > startPasteTick && x < endPasteTick).ToHashSet();
    }

}
// Each command has data that explains how to undo itself
// When command is executed, add that command to an array of commands, and have a function that calls the undo on whatever command is next in the array of commands when undo is clicked
// 