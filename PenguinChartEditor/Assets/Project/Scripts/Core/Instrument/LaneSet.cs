using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LaneSet<TValue> : IDictionary<int, TValue> where TValue : IEventData
{
    protected SortedDictionary<int, TValue> laneData;

    public LaneSet(LaneSet<TValue> copyData)
    {
        laneData = new(copyData.laneData);
    }

    public LaneSet()
    {
        laneData = new();
    }

    public TValue this[int key] 
    {
        get
        {
            return laneData[key];
        }
        set
        {
            laneData[key] = value;
        }
    }

    public ICollection<int> Keys 
    {
        get
        {
            return laneData.Keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            return laneData.Values;
        }
    }

    public int Count => laneData.Count;

    public bool IsReadOnly => false;

    public void Add(int key, TValue value)
    {
        laneData.Add(key, value);
    }

    public void Add(KeyValuePair<int, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear()
    {
        laneData.Clear();
    }

    public bool Contains(KeyValuePair<int, TValue> item)
    {
        return laneData.ContainsKey(item.Key);
    }

    public bool Contains(int tick)
    {
        return ContainsKey(tick);
    }

    public bool ContainsKey(int key)
    {
        return laneData.ContainsKey(key);
    }
    
    public bool ContainsTickInRangeExclusive(int startRange, int endRange)
    {
        var keyList = Keys.ToList();

        // startRange + 1 for an exclusive range
        var index = keyList.BinarySearch(startRange + 1);

        if (index > 0) index = ~index;

        // Index will either be the index of startRange + 1 (extremely unlikely)
        // or the next element larger than the start of the range.
        if (keyList[index] < endRange) return true;
        return false;
    }

    public bool ContainsTickInHopoRange(int startTick, bool positive)
    {
        if (positive) return ContainsTickInRangeExclusive(startTick, startTick + Chart.hopoCutoff);
        return ContainsTickInRangeExclusive(startTick, startTick - Chart.hopoCutoff);
    }

    public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex)
    {
        laneData.CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator()
    {
        return laneData.GetEnumerator();
    }

    public bool Remove(int tick)
    {
        return laneData.Remove(tick);
    }

    public bool Remove(KeyValuePair<int, TValue> item)
    {
        return laneData.Remove(item.Key);
    }

    public bool TryGetValue(int key, out TValue value)
    {
        return laneData.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}