using System.Collections;
using System.Collections.Generic;
using System.Linq;

// remember to set up TS/BPM
// do not use LaneSet.Add/LaneSet.Delete when doing batch add/delete => only first and last ticks need update trigger
public class LaneSet<TValue> : IDictionary<int, TValue> where TValue : IEventData
{
    protected SortedDictionary<int, TValue> laneData;
    public readonly HashSet<int> protectedTicks = new();

    public SortedDictionary<int, TValue> ExportData() => new(laneData);

    public LaneSet(LaneSet<TValue> copyData)
    {
        laneData = new(copyData.laneData);
    }

    public LaneSet(HashSet<int> protectedTicks)
    {
        laneData = new();
        this.protectedTicks = protectedTicks;
    }

    public LaneSet()
    {
        laneData = new();
    }

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

    public void Update(SortedDictionary<int, TValue> newEvents)
    {
        laneData = newEvents;
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

    public bool Remove(int tick)
    {
        return laneData.Remove(tick);
    }

    public bool Remove(int tick, out TValue data)
    {
        return laneData.Remove(tick, out data);
    }

    public SortedDictionary<int, TValue> SubtractSingle(int tick)
    {
        if (protectedTicks.Contains(tick)) return null;
        
        laneData.Remove(tick, out var data);
        return 
        new()
        {
            {tick, data}
        };
    }

    /// <summary>
    /// Returns removed ticks.
    /// </summary>
    /// <param name="tickData"></param>
    /// <returns></returns>
    public SortedDictionary<int, TValue> SubtractTicksFromSet(SortedDictionary<int, TValue> tickData)
    {
        SortedDictionary<int, TValue> subtractedTicks = new();
        foreach (var tick in tickData)
        {
            if (protectedTicks.Contains(tick.Key)) continue;

            if (Contains(tick.Key))
            {
                laneData.Remove(tick.Key, out TValue data);
                subtractedTicks.Add(tick.Key, data);
            }
        }
        return subtractedTicks;
    }

    public SortedDictionary<int, TValue> SubtractTicksInRange(int startTick, int endTick)
    {
        SortedDictionary<int, TValue> subtractedTicks = new();
        var ticksToDelete = GetOverwritableDictEvents(startTick, endTick);

        foreach (var tick in ticksToDelete)
        {
            if (protectedTicks.Contains(tick)) continue;

            if (Contains(tick))
            {
                laneData.Remove(tick, out TValue data);
                subtractedTicks.Add(tick, data);
            }
        }

        return subtractedTicks;
    }

    HashSet<int> GetOverwritableDictEvents(int startPasteTick, int endPasteTick)
    {
        return Keys.ToList().Where(x => x >= startPasteTick && x <= endPasteTick).ToHashSet();
    }

    #region Unmodified IDictionary Implementations

    public bool Remove(KeyValuePair<int, TValue> item)
    {
        return laneData.Remove(item.Key);
    }

    public bool TryGetValue(int key, out TValue value)
    {
        return laneData.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex)
    {
        laneData.CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator()
    {
        return laneData.GetEnumerator();
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

    #endregion
}