using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISelection
{
    bool Remove(int key);
}

/// <summary>
/// Wrapper for a SortedDictionary (key = tick, value = IEventData) that contains selection data.
/// Contains various QoL features for working with PCE selections.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class SelectionSet<TValue> : ISelection, ISet<int> where TValue : IEventData
{
    public const int NONE_SELECTED = -1;

    HashSet<int> selection = new();
    public int Count => selection.Count;


    LaneSet<TValue> parentLane;

    public SortedDictionary<int, TValue> ExportData()
    {
        SortedDictionary<int, TValue> receiver = new();
        foreach (var tick in selection)
        {
            if (parentLane.Contains(tick))
            {
                receiver.Add(tick, parentLane[tick]);
            }
            else
            {
                selection.Remove(tick);
            }
        }
        return receiver;
    }

    public SortedDictionary<int, TValue> ExportNormalizedData() => ExportNormalizedData(GetFirstSelectedTick());

    public SortedDictionary<int, TValue> ExportNormalizedData(int zeroTick)
    {
        var receiver = new SortedDictionary<int, TValue>();

        foreach (var selectedTick in selection)
        {
            if (parentLane.Contains(selectedTick))
            {
                receiver.Add(selectedTick - zeroTick, parentLane[selectedTick]);
            }
            else
            {
                selection.Remove(selectedTick);
            }
        }
        return receiver;
    }

    public HashSet<int> GetSelectedTicks()
    {
        foreach (var tick in selection)
        {
            if (!parentLane.Contains(tick))
            {
                selection.Remove(tick);
            }
        }
        return selection;
    }

    public SelectionSet(LaneSet<TValue> parentLane)
    {
        this.parentLane = parentLane;
    }

    public bool Add(int tick) => selection.Add(tick);

    public void AddInRange(int startTick, int endTick)
    {
        var targetTicks = parentLane.Keys.ToList().Where(x => x >= startTick && x <= endTick);
        foreach (var tick in targetTicks)
        {
            selection.Add(tick);
        }
    }

    public void ShiftClickSelectInRange(int startTick, int endTick)
    {
        selection.Clear();

        AddInRange(startTick, endTick);
    }

    public void Clear() => selection.Clear();

    public bool Contains(int key)
    {
        if (!parentLane.Contains(key))
        {
            selection.Remove(key);
        }

        return selection.Contains(key);
    }

    public bool Remove(int key) => selection.Remove(key);

    public void OverwriteWith(HashSet<int> newSelectionSet)
    {
        selection = new(newSelectionSet);
    }

    public void OverwriteWith(List<int> newSelectionSet)
    {
        selection = new(newSelectionSet);
    }

    public int GetFirstSelectedTick()
    {
        if (selection.Count > 0)
        {
            return selection.Min();
        }
        return NONE_SELECTED;
    }

    public HashSet<int> GetNormalizedSelection()
    {
        HashSet<int> receiver = new();
        var firstTick = GetFirstSelectedTick();

        foreach (var selectedTick in selection)
        {
            receiver.Add(selectedTick - firstTick);
        }
        return receiver;
    }

    public void ApplyScaledSelection(HashSet<int> normalizedSelection, int scalar)
    {
        HashSet<int> receiver = new();
        foreach (var item in normalizedSelection)
        {
            receiver.Add(item + scalar);
        }
        selection = new(receiver);
    }

    public void ApplyScaledSelection(SortedDictionary<int, TValue> normalizedSelection, int scalar) => ApplyScaledSelection(normalizedSelection.Keys.ToHashSet(), scalar);

    public void SelectAllInLane()
    {
        selection = new(parentLane.Keys);
    }

    public void UnionWith(IEnumerable<int> other) => selection.UnionWith(other);

    public SortedDictionary<int, TValue> PopSelectedTicksFromLane()
    {
        var subtractedData = parentLane.PopTicksFromSet(selection);
        selection.Clear();
        return subtractedData;
    }

    #region Unused Interface Implementations

    void ICollection<int>.Add(int tick) => Add(tick);

    IEnumerator IEnumerable.GetEnumerator() => selection.GetEnumerator();
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => selection.GetEnumerator();

    public bool IsReadOnly => false;

    public void ExceptWith(IEnumerable<int> other) => selection.ExceptWith(other);
    public void IntersectWith(IEnumerable<int> other) => selection.IntersectWith(other);
    public bool IsProperSubsetOf(IEnumerable<int> other) => selection.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<int> other) => selection.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<int> other) => selection.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<int> other) => selection.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<int> other) => selection.Overlaps(other);
    public bool SetEquals(IEnumerable<int> other) => selection.SetEquals(other);
    public void SymmetricExceptWith(IEnumerable<int> other) => selection.SymmetricExceptWith(other);
    public void CopyTo(int[] array, int arrayIndex) => selection.CopyTo(array, arrayIndex);


    #endregion
}