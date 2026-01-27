using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface ILaneData
{
    bool Contains(int tick);
    int GetNextRelevantTick();
    int GetFirstRelevantTick();
    int GetFirstRelevantTick(int targetTick);
    int GetTickSustain(int tick);
    int GetNextTickEventInLane(int currentTick, bool inclusive = false);
    int GetPreviousTickEventInLane(int currentTick, bool inclusive = false);
    List<int> GetRelevantTicksInRange(int startTick, int endTick);
}


// remember to set up TS/BPM
// do not use LaneSet.Add/LaneSet.Delete when doing batch add/delete => only first and last ticks need update trigger
public class LaneSet<TValue> : ILaneData, IDictionary<int, TValue> where TValue : IEventData
{
    public const int NO_TICK_EVENT = -1;
    private SortedDictionary<int, TValue> laneData;

    public delegate void UpdateNeededDelegate(int startTick, int endTick);

    /// <summary>
    /// Invoked whenever a hopo check needs to happen at a certain tick. 
    /// When invoked, the tick from the delegate should be checked to see if it or its surrounding ticks have changed hopo status.
    /// </summary>
    public event UpdateNeededDelegate UpdatesNeededInRange;

    /// <summary>
    /// Used to prevent the TS and BPM events at tick 0 from being deleted.
    /// If TS and BPM events at tick 0 are deleted, the chart has no place to start its beatline calculations from.
    /// [SyncTrack] must ALWAYS have one BPM and one TS event at tick 0.
    /// Users should edit tick 0 events for TS & BPM, not delete them.
    /// Also allows future devs to protect other ticks from deletion if need be.
    /// </summary>
    public readonly HashSet<int> protectedTicks = new();

    public SortedDictionary<int, TValue> ExportData() => new(laneData);
    public LaneSet(HashSet<int> protectedTicks)
    {
        laneData = new SortedDictionary<int, TValue>();
        this.protectedTicks = protectedTicks;
    }

    public LaneSet()
    {
        laneData = new SortedDictionary<int, TValue>();
    }

    public void Add(int key, TValue value)
    {
        if (key < 0) return;

        laneData.Remove(key);
        laneData.Add(key, value);
        UpdatesNeededInRange?.Invoke(key, key);
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

    // refactor this out - make redundant
    public void Update(SortedDictionary<int, TValue> newEvents)
    {
        laneData = newEvents;
    }
    
    public bool ContainsTickInRangeExclusive(int startRange, int endRange)
    {
        var keyList = Keys.ToList();

        // startRange + 1 for an exclusive range
        var index = keyList.BinarySearch(startRange + 1);

        if (index < 0) index = ~index;

        // Index will either be the index of startRange + 1 (extremely unlikely)
        // or the next element larger than the start of the range.
        if (keyList[index] < endRange) return true;
        return false;
    }

    public bool ContainsTickInHopoRange(int startTick, bool positive)
    {
        if (positive) return ContainsTickInRangeExclusive(startTick, startTick + Chart.HopoCutoff);
        return ContainsTickInRangeExclusive(startTick, startTick - Chart.HopoCutoff);
    }

    public bool Remove(int tick)
    {
        if (protectedTicks.Contains(tick))
        {
            return false;
        }

        var returnVal = laneData.Remove(tick);
        UpdatesNeededInRange?.Invoke(tick, tick);
        return returnVal;
    }

    public bool Remove(int tick, out TValue data)
    {
        if (protectedTicks.Contains(tick))
        {
            data = default;
            return false;
        }

        // remove must happen before update
        var returnVal = laneData.Remove(tick, out data);
        UpdatesNeededInRange?.Invoke(tick, tick);
        return returnVal;
    }

    public SortedDictionary<int, TValue> PopSingle(int tick)
    {
        if (protectedTicks.Contains(tick)) return null;

        laneData.Remove(tick, out var data);

        UpdatesNeededInRange?.Invoke(tick, tick);

        return 
        new SortedDictionary<int, TValue>
        {
            {tick, data}
        };
    }

    public void InvokeForSetEnds(SortedDictionary<int, TValue> subtractedTicksSet) => InvokeForSetEnds(subtractedTicksSet, offset: 0);

    public void InvokeForSetEnds(SortedDictionary<int, TValue> subtractedTicksSet, int offset)
    {
        if (subtractedTicksSet.Count == 0) return;

        var keys = subtractedTicksSet.Keys;

        Chart.Log($"{keys}");
        UpdatesNeededInRange(keys.Min(), keys.Max());
    }

    private void InvokeForSetEnds(HashSet<int> ticksAdded)
    {
        if (ticksAdded.Count == 0) return;

        UpdatesNeededInRange(ticksAdded.Min(), ticksAdded.Max());
    }

    /// <summary>
    /// Returns removed ticks.
    /// </summary>
    /// <param name="tickData"></param>
    /// <returns></returns>
    public SortedDictionary<int, TValue> PopTicksFromSet(SortedDictionary<int, TValue> tickData) => PopTicksFromSet(tickData.Select(kvp => kvp.Key).ToHashSet());

    public SortedDictionary<int, TValue> PopTicksFromSet(HashSet<int> tickData)
    {
        SortedDictionary<int, TValue> subtractedTicks = new();
        foreach (var tick in tickData)
        {
            if (protectedTicks.Contains(tick)) continue;

            if (Contains(tick))
            {
                laneData.Remove(tick, out TValue data);
                subtractedTicks.Add(tick, data);
            }
        }

        // InvokeForSetEnds(subtractedTicks);

        return subtractedTicks;
    }

    public SortedDictionary<int, TValue> PopTicksInRange(int startTick, int endTick)
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

        // InvokeForSetEnds(subtractedTicks);

        return subtractedTicks;
    }

    public void OverwriteTicksFromSet(HashSet<int> ticks, SortedDictionary<int, TValue> dataset)
    {
        foreach (var tick in ticks)
        {
            if (laneData.ContainsKey(tick))
            {
                laneData.Remove(tick);
            }
            if (!dataset.ContainsKey(tick)) continue;
            laneData.Add(tick, dataset[tick]);
        }
    }

    public void OverwriteLaneDataWith(SortedDictionary<int, TValue> data)
    {
        laneData = new SortedDictionary<int, TValue>(data);
    }

    public void OverwriteDataWithOffset(SortedDictionary<int, TValue> data, int tickOffset)
    {
        foreach (var tick in data)
        {
            var targetPasteTick = tickOffset + tick.Key;
            if (targetPasteTick < 0 || targetPasteTick > SongTime.SongLengthTicks) continue;
            if (laneData.ContainsKey(targetPasteTick))
            {
                laneData.Remove(targetPasteTick);
            }
            laneData.Add(targetPasteTick, tick.Value);
        }
    }


    private int BinarySearchForTick(int currentTick, out List<int> tickTimeKeys)
    {
        tickTimeKeys = Keys.ToList();
        return tickTimeKeys.BinarySearch(currentTick);
    }

    private HashSet<int> GetOverwritableDictEvents(int startPasteTick, int endPasteTick)
    {
        return Keys.ToList().Where(x => x >= startPasteTick && x <= endPasteTick).ToHashSet();
    }

    public int GetFirstRelevantTick() => GetFirstRelevantTick(SongTime.SongPositionTicks);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSustain">Any type of event data that has a sustain field. See FiveFretNoteData, GHLNoteData, etc.</typeparam>
    /// <param name="targetTick"></param>
    /// <returns>The next tick in this lane that needs to show feedback. 
    /// Returns the previous tick in the lane if the sustain of that note 
    /// is in progress at targetTick, which will need to show feedback.</returns>
    public int GetFirstRelevantTick(int targetTick)
    {
        // validate next and not previous tick because there is no need to
        // validate the previous tick in this scenario
        // if the previous tick is -1, then Contains fails every time 
        // (this<int>[] is effectively a uint unless something has gone terribly wrong)
        // but nextTick should return an unreachable tick so that the first relevant tick is effectively null
        // (theoretically, it is also possible to reach past the end of the song when something has gone terribly wrong)
        // since this function is used to return the next note the receivers care about,
        // returning a tick that will never be reached is a good substitute for null without breaking everything
        var previousTick = GetPreviousTickEventInLane(targetTick);
        var nextTick = ValidateEvent(GetNextTickEventInLane(targetTick, inclusive: true));

        if (Contains(previousTick) && this[previousTick] is ISustainable sustainDataContainer)
        {
            var prevSustainLength = sustainDataContainer.Sustain;

            var prevSustainEndPoint = previousTick + prevSustainLength;

            if (prevSustainEndPoint > SongTime.SongPositionTicks) return previousTick;
        }

        return nextTick;
    }

    public int GetNextRelevantTick() => GetNextRelevantTick(SongTime.SongPositionTicks);
    public int GetNextRelevantTick(int targetTick) => ValidateEvent(GetNextTickEventInLane(targetTick));

    private int ValidateEvent(int tickEvent) => tickEvent == NO_TICK_EVENT ? SongTime.SongLengthTicks + 1 : tickEvent;

    public int GetTickSustain(int tick)
    {
        if (Contains(tick) && this[tick] is ISustainable sustainData)
        {
            return sustainData.Sustain;
        }
        return 0;
    }

    /// <summary>
    /// Get the tick event before a specified tick. Returns -1 (LaneSet.NO_TICK_EVENT) if there is none.
    /// </summary>
    /// <param name="currentTick"></param>
    /// <returns></returns>
    public int GetPreviousTickEventInLane(int currentTick, bool inclusive = false)
    {
        if (inclusive && Contains(currentTick)) return currentTick;

        int index = BinarySearchForTick(currentTick, out var tickTimeKeys);

        // bitwise complement is negative
        if (index > 0) return inclusive ? tickTimeKeys[index] : tickTimeKeys[index - 1];

        if (~index == tickTimeKeys.Count) index = tickTimeKeys.Count - 1;
        else index = ~index - 1;

        if (index < 0) return NO_TICK_EVENT;
        return tickTimeKeys[index];
    }

    public int GetNextTickEventInLane(int currentTick, bool inclusive = false)
    {
        int index = BinarySearchForTick(currentTick, out var tickTimeKeys);

        if (~index == tickTimeKeys.Count || index >= tickTimeKeys.Count - 1) return NO_TICK_EVENT;

        // bitwise complement is negative
        if (index >= 0) return inclusive ? tickTimeKeys[index] : tickTimeKeys[index + 1];
        else index = ~index;

        return tickTimeKeys[index];
    }

    // Uses array and not list for easy range segmenting
    public List<int> GetRelevantTicksInRange(int startTick, int endTick)
    {
        if (Count == 0) return new List<int>();

        var tickList = Keys.ToList();

        var startIndex = tickList.BinarySearch(startTick);
        var endIndex = tickList.BinarySearch(endTick);

        if (startIndex < 0) startIndex = ~startIndex;
        if (endIndex < 0) endIndex = ~endIndex - 1;

        int sustainCandidateTick = startIndex == 0 ? tickList[startIndex] : tickList[startIndex - 1];

        if (this[sustainCandidateTick] is ISustainable tickSustainData)
        {
            if (sustainCandidateTick + tickSustainData.Sustain > startTick)
            {
                startIndex = startIndex == 0 ? startIndex : startIndex - 1;
            }
        }

        if (startIndex == tickList.Count || endIndex < 0)
        {
            return new List<int>();
        }

        var finalList = tickList.GetRange(startIndex, (endIndex + 1) - startIndex);
        return finalList;
    }


    #region Unmodified IDictionary Implementations

    public void Add(KeyValuePair<int, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public bool Remove(KeyValuePair<int, TValue> item)
    {
        return Remove(item.Key);
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
            if (key < 0)
            {
                throw new System.ArgumentException($"Tried to get a negative tick from a lane dictionary. Tick: {key}. If the key is -1, the most likely reason was because the method tried to get the previous event in a lane when there was none.");
            }
            return laneData[key];
        }
        set
        {
            if (key < 0) return;
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