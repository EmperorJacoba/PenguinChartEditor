using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public interface ILaneData : IEnumerable<TickPairing>
{
    bool Contains(int tick);
    int GetNextRelevantTick();
    int GetFirstRelevantTick();
    int GetFirstRelevantTick(int targetTick);
    int GetTickSustain(int tick);
    int GetNextTickEventInLane(int currentTick, bool inclusive = false);
    int GetPreviousTickEventInLane(int currentTick, bool inclusive = false);
    List<int> GetRelevantTicksInRange(int startTick, int endTick);

    bool Add(int tick, IEventData data);
    public bool CreateEvent(int tick, IEventData newData, out AddSingleDataPackage actionInfo);

    bool TryGetValue(int key, out IEventData data);
    
    bool Remove(int tick);

    HashSet<int> protectedTicks { get; }

    List<string> ToPenguinFormat();
}

public struct TickPairing
{
    public int tick;
    public IEventData data;

    public TickPairing(int tick, IEventData data)
    {
        this.tick = tick;
        this.data = data;
    }
}

// remember to set up TS/BPM
// do not use LaneSet.Add/LaneSet.Delete when doing batch add/delete => only first and last ticks need update trigger
public class LaneSet<TValue> : ILaneData, IDictionary<int, TValue> where TValue : IEventData
{
    #region Constants
    
    public const int NO_TICK_EVENT = -1;
    
    #endregion

    #region Underlying data

    private SortedDictionary<int, TValue> laneData;
    private readonly int laneID;
    
    /// <summary>
    /// Used to prevent the TS and BPM events at tick 0 from being deleted.
    /// If TS and BPM events at tick 0 are deleted, the chart has no place to start its beatline calculations from.
    /// [SyncTrack] must ALWAYS have one BPM and one TS event at tick 0.
    /// Users should edit tick 0 events for TS & BPM, not delete them.
    /// Also allows future devs to protect other ticks from deletion if need be.
    /// </summary>
    public HashSet<int> protectedTicks { get; } = new();
    
    public SortedDictionary<int, TValue> ExportData() => new(laneData);
    
    // refactor this out - make redundant
    public void Clear() => laneData.Clear();
    public void Update(SortedDictionary<int, TValue> newEvents) => laneData = newEvents;
    

    #endregion

    #region UpdatesNeededInRange

    // This needs to be removed. All add actions go through different spots now and should NEVER happen through LaneSet
    
    public delegate void UpdateNeededDelegate(int startTick, int endTick);

    /// <summary>
    /// Invoked whenever a hopo check needs to happen at a certain tick. 
    /// When invoked, the tick from the delegate should be checked to see if it or its surrounding ticks have changed hopo status.
    /// </summary>
    public event UpdateNeededDelegate UpdatesNeededInRange;

    #endregion
    
    #region Constructor
    
    public LaneSet(int laneID, HashSet<int> protectedTicks)
    {
        this.laneID = laneID;
        
        laneData = new SortedDictionary<int, TValue>();
        this.protectedTicks = protectedTicks;
    }

    public LaneSet(int laneID)
    {
        this.laneID = laneID;
        
        laneData = new SortedDictionary<int, TValue>();
    }

    public LaneSet(LaneSet<TValue> laneSet)
    {
        laneID = laneSet.laneID;
        laneData = new SortedDictionary<int, TValue>(laneSet.laneData);
        protectedTicks = laneSet.protectedTicks;
    }

    public LaneSet(int laneID, string[] lines, HashSet<int> protectedTicks = null)
    {
        this.laneID = laneID;
        
        if (protectedTicks is not null)
        {
            this.protectedTicks = protectedTicks;
        }
        
        laneData = 
            new SortedDictionary<int, TValue>(
                lines.Select(x => x.Split(" = ", 1)).
                    ToDictionary(
                        x => int.Parse(x[0]), 
                        x => EventDataFactory.ConvertToEventData<TValue>(x[1])
                    )
                );
    }
    
    #endregion
    
    #region Export

    public List<string> ToPenguinFormat()
    {
        return laneData.Select(kvp => $"\t\t{kvp.Key} = {kvp.Value.ToPenguinFormat()}").ToList();
    }

    #endregion
    
    #region Add

    bool ILaneData.Add(int tick, IEventData data) => Add(tick, (TValue)data);
    void IDictionary<int, TValue>.Add(int key, TValue value) => Add(key, value);
    public bool Add(int key, TValue value)
    {
        if (key < 0) return false;

        if (laneData.TryGetValue(key, out var data))
        {
            if (data.Equals(value))
            {
                return false;
            }
        }

        laneData[key] = value;

        UpdatesNeededInRange?.Invoke(key, key);

        return true;
    }

    public bool CreateEvent(int tick, IEventData newData, out AddSingleDataPackage actionInfo) => 
        TypedCreateEvent(tick, (TValue)newData, out actionInfo);
    
    public bool TypedCreateEvent(int tick, TValue newData, out AddSingleDataPackage actionInfo)
    {
        actionInfo = new AddSingleDataPackage(tick, laneID, newData);

        if (tick < 0) return false;
        
        if (laneData.TryGetValue(tick, out var oldData))
        {
            if (oldData.Equals(newData))
            {
                return false;
            }

            actionInfo = new AddSingleDataPackage(tick, laneID, newData, oldData);
        }

        laneData[tick] = newData;
        
        return true;
    }
    
    public void AddTicksFromSet(SortedDictionary<int, TValue> dataset)
    {
        if (dataset is null) return;
        
        foreach (var item in dataset)
        {
            laneData[item.Key] = item.Value;
        }
    }

    public void AddTicksFromSet(SortedDictionary<int, TValue> dataset,
        out SortedDictionary<int, TValue> overwrittenData)
    {
        overwrittenData = new SortedDictionary<int, TValue>();
        foreach (var item in dataset)
        {
            if (laneData.TryGetValue(item.Key, out var data))
            {
                overwrittenData[item.Key] = data;
            }

            laneData[item.Key] = item.Value;
        }
    }
    
    #endregion

    #region Contains
    
    public bool Contains(KeyValuePair<int, TValue> item) => laneData.ContainsKey(item.Key);
    public bool Contains(int tick) => ContainsKey(tick);
    public bool ContainsKey(int key) => laneData.ContainsKey(key);

    public bool ContainsTickInRangeExclusive(int startRange, int endRange)
    {
        var keyList = Keys.ToList();

        // startRange + 1 for an exclusive range
        var index = keyList.BinarySearch(startRange + 1);

        if (index < 0) index = ~index;

        // Index will either be the index of startRange + 1 (extremely unlikely)
        // or the next element larger than the start of the range.
        return keyList[index] < endRange;
    }
    
    #endregion
    
    #region TryGet

    public bool TryGetValue(int key, out IEventData data)
    {
        data = null;
        if (laneData.TryGetValue(key, out var value))
        {
            data = value;
            return true;
        }

        return false;
    }

    #endregion
    
    #region Remove

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
    
    public void DeleteTicksFromSet(IEnumerable<int> keys)
    {
        foreach (var tick in keys)
        {
            laneData.Remove(tick);
        }
    }
    
    #endregion

    #region Pop

    #region Single
    
    public IEventData PopSingle(int tick)
    {
        if (protectedTicks.Contains(tick)) return null;
        
        laneData.Remove(tick, out var data);

        UpdatesNeededInRange?.Invoke(tick, tick);

        return data;
    }

    public TValue PopSingleTyped(int tick)
    {
        if (protectedTicks.Contains(tick)) return default;
        
        laneData.Remove(tick, out var data);

        UpdatesNeededInRange?.Invoke(tick, tick);

        return data;
    }
    
    #endregion

    /// <summary>
    /// Returns removed ticks.
    /// </summary>
    /// <param name="tickData"></param>
    /// <returns></returns>
    public SortedDictionary<int, TValue> PopTicksFromSet(SortedDictionary<int, TValue> tickData) => 
        PopTicksFromSet(tickData.Select(kvp => kvp.Key).ToHashSet());

    public SortedDictionary<int, TValue> PopTicksFromSet(HashSet<int> tickData)
    {
        SortedDictionary<int, TValue> subtractedTicks = new();
        foreach (var tick in tickData.Where(tick => !protectedTicks.Contains(tick) && Contains(tick)))
        {
            laneData.Remove(tick, out var data);
            subtractedTicks.Add(tick, data);
        }
        
        return subtractedTicks;
    }

    public SortedDictionary<int, TValue> PopTicksInRange(MinMaxTicks minMaxTicks) =>
        PopTicksInRange(minMaxTicks.min, minMaxTicks.max);
    
    public SortedDictionary<int, TValue> PopTicksInRange(int startTick, int endTick)
    {
        SortedDictionary<int, TValue> subtractedTicks = new();
        var ticksToDelete = GetOverwritableDictEvents(startTick, endTick);

        foreach (var tick in ticksToDelete.Where(tick => !protectedTicks.Contains(tick) && Contains(tick)))
        {
            laneData.Remove(tick, out var data);
            subtractedTicks.Add(tick, data);
        }
        
        return subtractedTicks;
    }
    
    private HashSet<int> GetOverwritableDictEvents(int startPasteTick, int endPasteTick) => 
        Keys.ToList().Where(x => x >= startPasteTick && x <= endPasteTick).ToHashSet();
    
    #endregion

    #region Peek
    
    public SortedDictionary<int, TValue> PeekTicksInRange(int startTick, int endTick)
    {
        return new SortedDictionary<int, TValue>(
                laneData.Where(
                    kvp => kvp.Key >= startTick && kvp.Key <= endTick
                    ).ToDictionary(
                    kvp => kvp.Key, kvp => kvp.Value
                    )
                );
    }
    
    #endregion

    #region Overwrite
    
    public void OverwriteTicksFromSet(HashSet<int> ticks, SortedDictionary<int, TValue> dataset)
    {
        foreach (var tick in ticks)
        {
            laneData.Remove(tick);
            
            if (!dataset.TryGetValue(tick, out var value)) continue;
            
            laneData.Add(tick, value);
        }
    }

    public void OverwriteAllLaneDataWith(SortedDictionary<int, TValue> data) => 
        laneData = new SortedDictionary<int, TValue>(data);

    public void OverwriteDataWithOffset(SortedDictionary<int, TValue> data, int tickOffset)
    {
        foreach (var tick in data)
        {
            var targetTick = tickOffset + tick.Key;
            if (targetTick < 0 || targetTick > SongTime.SongLengthTicks) continue;

            laneData[targetTick] = tick.Value;
        }
    }
    
    #endregion
    
    #region Relevant Tick
    
    private static int ValidateEvent(int tickEvent) => tickEvent == NO_TICK_EVENT ? SongTime.SongLengthTicks + 1 : tickEvent;

    public int GetFirstRelevantTick() => GetFirstRelevantTick(SongTime.SongPositionTicks);
    
    /// <param name="targetTick"></param>
    /// <returns>The next tick in this lane that needs to show feedback. 
    /// Returns the previous tick in the lane if the sustain of that note 
    /// is in progress at targetTick, which will need to show feedback.</returns>
    public int GetFirstRelevantTick(int targetTick)
    {
        // negative ints can be passed in here but that will cause this to die in a fiery flame
        targetTick = Mathf.Max(0, targetTick);
        
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
    
    #endregion

    #region Tick Sustain
    
    public int GetTickSustain(int tick)
    {
        if (Contains(tick) && this[tick] is ISustainable sustainData)
        {
            return sustainData.Sustain;
        }
        return 0;
    }
    
    #endregion

    #region Tick triangulation
    
    private int BinarySearchForTick(int currentTick, out List<int> tickTimeKeys)
    {
        tickTimeKeys = Keys.ToList();
        return tickTimeKeys.BinarySearch(currentTick);
    }

    /// <summary>
    /// Get the tick event before a specified tick. Returns -1 (LaneSet.NO_TICK_EVENT) if there is none.
    /// </summary>
    /// <returns></returns>
    public int GetPreviousTickEventInLane(int currentTick, bool inclusive = false)
    {
        if (inclusive && Contains(currentTick)) return currentTick;

        int index = BinarySearchForTick(currentTick, out var tickTimeKeys);

        // bitwise complement is negative
        if (index > 0) return inclusive ? tickTimeKeys[index] : tickTimeKeys[index - 1];

        if (~index == tickTimeKeys.Count) index = tickTimeKeys.Count - 1;
        else index = ~index - 1;

        return index < 0 ? NO_TICK_EVENT : tickTimeKeys[index];
    }
    
    public int GetNextTickEventInLane(int currentTick, bool inclusive = false)
    {
        int index = BinarySearchForTick(currentTick, out var tickTimeKeys);

        if (~index == tickTimeKeys.Count || index >= tickTimeKeys.Count - 1) return NO_TICK_EVENT;

        // bitwise complement is negative
        if (index >= 0) return inclusive ? tickTimeKeys[index] : tickTimeKeys[index + 1];
        
        index = ~index;

        return tickTimeKeys[index];
    }

    public SortedDictionary<int, TValue> SelectTicksFromSet(List<int> ticks)
    {
        var outputDict = new SortedDictionary<int, TValue>();
        foreach (var tick in ticks)
        {
            if (TryGetValue(tick, out TValue data))
            {
                outputDict.Add(tick, data);
            }
        }

        return outputDict;
    }

    // Uses array and not list for easy range segmenting, accounts for sustains
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
    
    #endregion
    
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

    IEnumerator<TickPairing> IEnumerable<TickPairing>.GetEnumerator()
    {
        return laneData.Select(note => new TickPairing(note.Key, note.Value)).GetEnumerator();
    }
}