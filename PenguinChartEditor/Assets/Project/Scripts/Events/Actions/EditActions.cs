using System.Collections.Generic;

// Based on https://refactoring.guru/design-patterns/command
public interface IEditAction<T> where T : IEventData
{
    public void Undo();
    public SortedDictionary<int, T> SaveData { get; set; }
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