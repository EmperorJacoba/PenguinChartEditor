using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Based on https://refactoring.guru/design-patterns/command
public interface IEditAction<DataType>
{
    public void Undo();
    public SortedDictionary<int, DataType> SaveData { get; set; }
}

public class Copy<DataType> : IEditAction<DataType>
{
    SortedDictionary<int, DataType> clipboard;
    HashSet<int> selection;
    SortedDictionary<int, DataType> targetEventSet;
    public SortedDictionary<int, DataType> SaveData { get; set; }

    public Copy(SortedDictionary<int, DataType> clipboard, HashSet<int> selection, SortedDictionary<int, DataType> targetEventSet)
    {
        this.clipboard = clipboard;
        this.selection = selection;
        this.targetEventSet = targetEventSet;
    }

    public bool Execute()
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
        return false;
    }

    public void Undo()
    {
        return;
    }
}

public class Paste<DataType> : IEditAction<DataType>
{
    public SortedDictionary<int, DataType> SaveData { get; set; }

    public SortedDictionary<int, DataType> Execute(int startPasteTick, SortedDictionary<int, DataType> clipboard, SortedDictionary<int, DataType> targetEventSet)
    {
        SortedDictionary<int, DataType> newDict;
        if (clipboard.Count > 0) // avoid index error
        {
            // Create a temp dictionary without events within the size of the clipboard from the origin of the paste 
            // (ex. clipboard with 0, 100, 400 has a zone of 400, paste starts at tick 700, all events tick 700-1100 are wiped)
            var endPasteTick = clipboard.Keys.Max() + startPasteTick;
            SortedDictionary<int, DataType> tempDict = GetNonOverwritableDictEvents(targetEventSet, startPasteTick, endPasteTick);

            // Add clipboard data to temp dict, now cleaned of obstructing events
            foreach (var clippedTick in clipboard)
            {
                tempDict.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
                SaveData.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
            }
            // Commit the temporary dictionary to the real dictionary
            // (cannot use targetEventSet as that results with a local reassignment)
            newDict = tempDict;
            SaveData = tempDict;
        }
        else newDict = targetEventSet;

        return newDict;
    }

    public void Undo()
    {

    }

    

    
    /// <summary>
    /// Copy all events from an event dictionary that are not within a paste zone (startTick to endTick)
    /// </summary>
    /// <typeparam name="Key">Key is a tick (int)</typeparam>
    /// <typeparam name="DataType">Event data -> ex. TS: (int, int)</typeparam>
    /// <param name="originalDict">Target dictionary of events to extract from.</param>
    /// <param name="startTick">Start of paste zone (events to overwrite)</param>
    /// <param name="endTick">End of paste zone (events to overwrite)</param>
    /// <returns>Event dictionary with all events in the paste zone removed.</returns>
    protected SortedDictionary<int, DataType> GetNonOverwritableDictEvents(SortedDictionary<int, DataType> originalDict, int startTick, int endTick)
    {
        SortedDictionary<int, DataType> tempDictionary = new();
        foreach (var item in originalDict)
        {
            if (item.Key < startTick || item.Key > endTick) tempDictionary.Add(item.Key, item.Value);
        }
        return tempDictionary;
    }
}

// Each command has data that explains how to undo itself
// When command is executed, add that command to an array of commands, and have a function that calls the undo on whatever command is next in the array of commands when undo is clicked
// 