using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ClipboardSet<TValue> : IDictionary<int, TValue> where TValue : IEventData
{
    SortedDictionary<int, TValue> clipboard = new();
    SortedDictionary<int, TValue> parentLane;

    public ClipboardSet(SortedDictionary<int, TValue> parentLane)
    {
        this.parentLane = parentLane;
    }

    public TValue this[int key]
    {
        get
        {
            return clipboard[key];
        }
        set
        {
            clipboard[key] = value;
        }
    }

    public ICollection<int> Keys
    {
        get
        {
            return clipboard.Keys;
        }
        set
        {
            SortedDictionary<int, TValue> receiver = new();
            foreach (var item in value)
            {
                receiver.Add(item, parentLane[item]);
            }
            clipboard = receiver;
        }
    }

    public void SetClipboard(List<int> ticks)
    {
        Keys = ticks;
    }

    public ICollection<TValue> Values => clipboard.Values;

    public int Count => clipboard.Count;

    public bool IsReadOnly => false;

    public void Add(int key, TValue value)
    {
        if (clipboard.ContainsKey(key)) clipboard.Remove(key);
        clipboard.Add(key, value);
    }

    public void Add(KeyValuePair<int, TValue> item) => Add(item.Key, item.Value);
    public void Add(int tick)
    {
        if (parentLane.ContainsKey(tick))
        {
            clipboard.Add(tick, parentLane[tick]);
        }
    }

    public void AddInRange(int startTick, int endTick)
    {
        var targetTicks = parentLane.Keys.ToList().Where(x => x >= startTick && x <= endTick);
        foreach (var tick in targetTicks)
        {
            clipboard.Add(tick, parentLane[tick]);
        }
    }

    public void Clear()
    {
        if (clipboard.Count > 0) clipboard.Clear();
    }

    public bool Contains(KeyValuePair<int, TValue> item) => clipboard.ContainsKey(item.Key);
    public bool Contains(int key) => clipboard.ContainsKey(key);
    public bool ContainsKey(int key) => clipboard.ContainsKey(key);

    public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex) => clipboard.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator() => clipboard.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public bool Remove(int key)
    {
        if (clipboard.ContainsKey(key))
        {
            clipboard.Remove(key);
            return true;
        }
        return false;
    }

    public bool Remove(KeyValuePair<int, TValue> item) => clipboard.Remove(item.Key);

    public bool TryGetValue(int key, out TValue value) => clipboard.TryGetValue(key, out value);

    public void Overwrite(IDictionary<int, TValue> newClipboardSet)
    {
        clipboard = new(newClipboardSet);
    }

    public int GetLastClipboardKey()
    {
        return clipboard.Keys.ToList()[^1];
    }
}