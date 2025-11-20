using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Wrapper for a SortedDictionary (key = tick, value = IEventData) that contains selection data.
/// Contains various QoL features for working with PCE selections.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class SelectionSet<TValue> : IDictionary<int, TValue> where TValue : IEventData
{
    public const int NONE_SELECTED = -1;

    SortedDictionary<int, TValue> selection = new();
    LaneSet<TValue> parentLane;

    public SortedDictionary<int, TValue> ExportData() => new(selection);

    public SelectionSet(LaneSet<TValue> parentLane)
    {
        this.parentLane = parentLane;
    }

    public TValue this[int key] 
    { 
        get 
        {
            return selection[key];
        }
        set 
        {
            selection[key] = value;
        } 
    }

    /// <summary>
    /// Set: Add all ints in collection to selection
    /// </summary>
    public ICollection<int> Keys
    {
        get
        {
            return selection.Keys;
        }
        set
        {
            SortedDictionary<int, TValue> receiver = new();
            foreach (var item in value)
            {
                receiver.Add(item, parentLane[item]);
            }
            selection = receiver;
        }
    }

    public void SetSelection(List<int> ticks)
    {
        Keys = ticks;
    }

    public ICollection<TValue> Values => selection.Values;

    public int Count => selection.Count;

    public bool IsReadOnly => false;

    /// <summary>
    /// Adds tick
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(int key, TValue value)
    {
        if (selection.ContainsKey(key)) selection.Remove(key);
        selection.Add(key, value);
    }

    public void Add(KeyValuePair<int, TValue> item) => Add(item.Key, item.Value);

    public void Add(int tick)
    {
        if (parentLane.ContainsKey(tick))
        {
            selection.Remove(tick);
        }

        if (parentLane.Contains(tick))
            selection.Add(tick, parentLane[tick]);
    }

    public void AddInRange(int startTick, int endTick)
    {
        var targetTicks = parentLane.Keys.ToList().Where(x => x >= startTick && x <= endTick);
        foreach (var tick in targetTicks)
        {
            selection.Add(tick, parentLane[tick]);
        }
    }

    public void ShiftClickSelectInRange(int startTick, int endTick)
    {
        selection.Clear();

        AddInRange(startTick, endTick);
    }

    public void Clear() 
    {
        if (selection.Count > 0) selection.Clear();
    }

    public bool Contains(KeyValuePair<int, TValue> item) => selection.ContainsKey(item.Key);
    public bool Contains(int key) => selection.ContainsKey(key);

    public bool ContainsKey(int key) => selection.ContainsKey(key);

    public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex) => selection.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator() => selection.GetEnumerator();

    public bool Remove(int key) 
    {
        if (selection.ContainsKey(key))
        {
            selection.Remove(key);
            return true;
        }
        return false;
    }

    public bool Remove(KeyValuePair<int, TValue> item) => selection.Remove(item.Key);

    public bool TryGetValue(int key, out TValue value) => selection.TryGetValue(key, out value);
    public void OverwriteWith(IDictionary<int, TValue> newSelectionSet)
    {
        selection = new(newSelectionSet);
    }

    public int GetFirstSelectedTick()
    {
        if (selection.Count > 0)
        {
            return selection.Keys.Min();
        }
        return NONE_SELECTED;
    }

    public SortedDictionary<int, TValue> GetNormalizedSelection()
    {
        SortedDictionary<int, TValue> receiver = new();
        var firstTick = GetFirstSelectedTick();
        foreach (var selectedTick in selection)
        {
            receiver.Add(selectedTick.Key - firstTick, selectedTick.Value);
        }
        return receiver;
    }

    public void ApplyScaledSelection(SortedDictionary<int, TValue> normalizedSelection, int scalar)
    {
        SortedDictionary<int, TValue> receiver = new();
        foreach (var item in normalizedSelection)
        {
            receiver.Add(item.Key + scalar, item.Value);
        }
        selection = new(receiver);
    }

    public void SelectAll()
    {
        selection = new(parentLane);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}