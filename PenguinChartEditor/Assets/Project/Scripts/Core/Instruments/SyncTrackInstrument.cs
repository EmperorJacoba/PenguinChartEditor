using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class SyncTrackInstrument : BaseInstrument<BPMData>
{
    #region Constants

    private const int SECONDS_PER_MINUTE = 60;
    private const string ANCHOR_IDENTIFIER = "A";
    private const float MINIMUM_BPM_VALUE = 0;
    private const float MAXIMUM_BPM_VALUE = 1000;
    private const int DEFAULT_TS_DENOMINATOR = 4;
    private const string TEMPO_EVENT_INDICATOR = "B";
    private const string TIME_SIGNATURE_EVENT_INDICATOR = "TS";
    private const string SYNC_TRACK_ERROR = "[SyncTrack] has invalid tempo event:";
    private const int MICROSECOND_CONVERSION = 1000000;
    private const float BPM_FORMAT_CONVERSION = 1000.0f;
    private const int TS_POWER_CONVERSION_NUMBER = 2;

    #endregion

    #region Data/Setup

    protected override IMultiLaneController LaneController => Lanes;
    private SyncTrackLanes Lanes;

    // Data types are fundamentally different, very hard to combine into one single Lanes object
    // Both are also structs because of their small (and repeatable) size.
    // Converting from IEventData to XData every time you want to get a statistic would be too much overhead. 
    public LaneSet<BPMData> TempoEvents => Lanes.TempoEvents;
    public LaneSet<TSData> TimeSignatureEvents => Lanes.TimeSignatureEvents;

    public override ILaneData GetBarLaneData() => throw new NullReferenceException("SyncTrackInstrument does not have a bar lane, as it does not use traditional instrument lanes.");
    
    public override SoloDataSet SoloData
    {
        get => null;
        set {}
    }

    public SyncTrackInstrument(List<KeyValuePair<int, string>> fileData)
    {
        InstrumentName = InstrumentType.synctrack;
        Difficulty = DifficultyType.easy;

        Lanes = new SyncTrackLanes();

        AddChartFormattedEventsToInstrument(fileData);
        if (TempoEvents.Count == 0)
        {
            TempoEvents[0] = new BPMData(120, 0, false);
        }

        if (TimeSignatureEvents.Count == 0)
        {
            TimeSignatureEvents[0] = new TSData(4, 4);
        }
    }

    public enum LaneOrientation
    {
        bpm = 0,
        timeSignature = 1
    }

    public override ISelectionSnapshot GetEmptySelectionSnapshot()
    {
        return new SyncTrackSelectionSnapshot(new SortedDictionary<int, BPMData>(),
            new SortedDictionary<int, TSData>());
    }

    #endregion

    #region Movement

    protected override bool IsMoveActionValid() => !Input.GetKey(KeyCode.LeftControl);

    protected override void InternalMoveSelectionChecks()
    {
        RecalculateTempoEventDictionary();
        Chart.SyncTrackInPlaceRefresh();
    }

    #endregion

    #region Add/Delete

    protected override void InternalAddDataChecks(int tick, int lane) {}

    // FIXME: Pass something in here so that RTED (expensive call) only runs when tempo events are modified
    protected override void InternalDeleteChecks()
    {
        RecalculateTempoEventDictionary();
        Chart.SyncTrackInPlaceRefresh();
    }

    #endregion

    #region Undo/Redo

    protected override void InternalSaveUndoData(UndoSnapshot<BPMData> undoAction) => throw new NotImplementedException();
    protected override void InternalApplyUndoAction(UndoSnapshot<BPMData> undoAction) => throw new NotImplementedException();


    protected override IUndoSnapshot CreateUndoSnapshot()
    {
        return new SyncTrackUndoSnapshot(TempoEvents, TimeSignatureEvents);
    }
    

    public override void PushUndoData(IUndoSnapshot undoSnapshot)
    {
        var snapshot = undoSnapshot as SyncTrackUndoSnapshot;

        TempoEvents.OverwriteAllLaneDataWith(snapshot.bpmSave);
        TimeSignatureEvents.OverwriteAllLaneDataWith(snapshot.tsSave);
    }

    #endregion

    #region Tempo

    public int GetNextAnchor(int currentTick)
    {
        var nextAnchors = TempoEvents.Where(item => item.Key > currentTick && item.Value.Anchor).ToList();
        if (nextAnchors.Count > 0) return nextAnchors[0].Key;
        else return -1;
    }
    public int GetLastAnchor(int currentTick)
    {
        var lastAnchors = TempoEvents.Where(item => item.Key < currentTick && item.Value.Anchor).ToList();
        if (lastAnchors.Count > 0) return lastAnchors[^1].Key;
        else return -1;
    }

    // This anchoring logic may present some accuracy errors in the dictionary
    // *should* only be microseconds at most but logic may need to be rethought if possible
    // maybe re-validate dictionary when exporting?
    // Effects currently unknown, but round off should fix it if anything
    // Please remove and re-think if any errors arise from exporting to different software/YARG/Clone Hero
    // NO EVIDENCE FOR INACCURACY AT THIS TIME - if anything recalculations should resolve any
    public float CalculateLastBPMBeforeAnchor(int currentTick, float newTime)
    {
        var nextAnchor = GetNextAnchor(currentTick);
        return CalculateLastBPMBeforeAnchor(currentTick, newTime, nextAnchor);
    }

    public float CalculateLastBPMBeforeAnchor(int currentTick, float newTime, int nextAnchor)
    {
        float anchoredBPS = ((nextAnchor - currentTick) / (float)Chart.Resolution) / (TempoEvents[nextAnchor].Timestamp - newTime);
        float anchoredBPM = (float)Math.Round((anchoredBPS * 60), 3);
        return anchoredBPM;
    }

    public void RecalculateTempoEventDictionary(int modifiedTick, float timeChange)
    {
        SortedDictionary<int, BPMData> outputTempoEventsDict = new();

        var tickEvents = TempoEvents.Keys.ToList();
        var positionOfTick = tickEvents.FindIndex(x => x == modifiedTick);
        if (positionOfTick == tickEvents.Count - 1) return; // no events to modify

        // Keep all events before change when creating new dictionary
        // Manage anchors in BPMLabel.ChangePositionFromDrag() - much simpler
        for (int i = 0; i <= positionOfTick; i++)
        {
            outputTempoEventsDict.Add(tickEvents[i], TempoEvents[tickEvents[i]]);
        }

        // Start new data with the song timestamp of the change
        double currentSongTime = outputTempoEventsDict[tickEvents[positionOfTick]].Timestamp;
        for (int i = positionOfTick + 1; i < tickEvents.Count; i++)
        {
            var bpmChange = TempoEvents[tickEvents[i]].BPMChange;

            if (tickEvents.Count > (i + 1)) // validation check - no anchor will be ahead of the last event
            {
                // anchor calculations happen on the bpm event before an anchor, 
                // so instead of catching the anchor when we get to it, catch it before to avoid multiple writes to the same index
                if (TempoEvents[tickEvents[i + 1]].Anchor)
                {
                    bpmChange = CalculateLastBPMBeforeAnchor(tickEvents[i], TempoEvents[tickEvents[i]].Timestamp + timeChange);
                }
            }

            // anchor = time no changey
            if (TempoEvents[tickEvents[i]].Anchor)
            {
                timeChange = 0;
            }

            outputTempoEventsDict.Add(tickEvents[i], new BPMData(bpmChange, TempoEvents[tickEvents[i]].Timestamp + timeChange, TempoEvents[tickEvents[i]].Anchor));
        }

        TempoEvents.Update(outputTempoEventsDict);
    }

    /// <summary>
    /// Recalculate all tempo events from the tick-time timestamp modified onward.
    /// </summary>
    /// <param name="modifiedTick">The last tick modified to update all future ticks from.</param>
    public void RecalculateTempoEventDictionary(int modifiedTick = 0)
    {
        SortedDictionary<int, BPMData> outputTempoEventsDict = new();

        var tickEvents = TempoEvents.Keys.ToList();
        var positionOfTick = tickEvents.FindIndex(x => x == modifiedTick);
        if (positionOfTick == tickEvents.Count - 1) return; // no events to modify

        // Keep all events before change when creating new dictionary
        for (int i = 0; i <= positionOfTick; i++)
        {
            var tick = tickEvents[i];
            var bpmData = TempoEvents[tickEvents[i]];
            var timestamp = tick == 0 ? new BPMData(bpmData.BPMChange, 0, bpmData.Anchor) : TempoEvents[tickEvents[i]];

            outputTempoEventsDict.Add(tick, timestamp);
        }
        
        // Start new data with the song timestamp of the change
        double currentSongTime = outputTempoEventsDict[tickEvents[positionOfTick]].Timestamp;
        for (int i = positionOfTick + 1; i < tickEvents.Count; i++)
        {
            double calculatedTimeSecondDifference = 0;

            if (i > 0)
            {
                // Taken from Chart File Format Specifications -> Calculate time from one pos to the next at a constant bpm
                calculatedTimeSecondDifference =
                (tickEvents[i] - tickEvents[i - 1]) / (double)Chart.Resolution * 60 / TempoEvents[tickEvents[i - 1]].BPMChange;
            }

            currentSongTime += calculatedTimeSecondDifference;
            outputTempoEventsDict.Add(tickEvents[i], new BPMData(TempoEvents[tickEvents[i]].BPMChange, (float)currentSongTime, TempoEvents[tickEvents[i]].Anchor));
        }

        TempoEvents.Update(outputTempoEventsDict);
    }

    // BPM can't be negative and event selection gets screwed with when the BPM is too high
    public bool IsTickInBounds(float bpm) => bpm > MINIMUM_BPM_VALUE && bpm < MAXIMUM_BPM_VALUE;

    /// <summary>
    /// Take a number of seconds (in S.ms form - ex. 61.1 seconds) and convert it to MM:SS.mmm format (where 61.1 returns 01:01.100)
    /// </summary>
    /// <param name="position">The unformatted second count.</param>
    /// <returns>The formatted MM:SS:mmm timestamp of the second position</returns>
    public string ConvertSecondsToTimestamp(double position)
    {
        var minutes = Math.Floor(position / 60);
        var secondsWithMS = position - minutes * 60;
        var seconds = (int)Math.Floor(secondsWithMS);
        var milliseconds = Math.Round(secondsWithMS - seconds, 3) * 1000;

        string minutesString = minutes.ToString();
        if (minutes < 10)
        {
            minutesString = minutesString.PadLeft(minutesString.Length + 1, '0');
        }

        string secondsString = seconds.ToString();
        if (seconds < 10)
        {
            secondsString = secondsString.PadLeft(2, '0');
        }

        string millisecondsString = milliseconds.ToString();
        if (millisecondsString.Length < 3)
        {
            millisecondsString = millisecondsString.PadRight(3, '0');
        }

        return minutesString + ":" + secondsString + "." + millisecondsString;
    }

    public int ConvertSecondsToTickTime(float timestamp)
    {
        if (timestamp <= 0)
            return 0;

        if (timestamp > AudioManager.SongLength)
            return SongTime.SongLengthTicks;

        var tempoTickTimeEvents = TempoEvents.Keys.ToList();
        var tempoTimeSecondEvents = TempoEvents.Values.Select(x => x.Timestamp).ToList();

        var index = tempoTimeSecondEvents.BinarySearch(timestamp);

        int lastTickEvent;
        if (index < 0) // bitwise complement is negative or zero
        {
            if (~index == tempoTimeSecondEvents.Count) index = tempoTimeSecondEvents.Count - 1;
            else index = ~index - 1;

            if (index < 0)
            {
                lastTickEvent = tempoTickTimeEvents[0];
            }
            else
            {
                lastTickEvent = tempoTickTimeEvents[index];
            }
        }
        else
        {
            lastTickEvent = tempoTickTimeEvents[index];
        }

        var dataRef = TempoEvents[lastTickEvent];
        // Rearranging of .chart format specification distance between two ticks - thanks, algebra class!
        return Mathf.RoundToInt((Chart.Resolution * dataRef.BPMChange * (float)(timestamp - dataRef.Timestamp) / SECONDS_PER_MINUTE) + lastTickEvent);
    }

    public double ConvertTickTimeToSeconds(int tickTime)
    {
        if (tickTime == 0) return 0;

        var lastTickEvent = TempoEvents.GetPreviousTickEventInLane(tickTime, inclusive: true);
        
        // Formula from .chart format specifications
        var dataRef = TempoEvents[lastTickEvent];
        return ((tickTime - lastTickEvent) / (double)Chart.Resolution * SECONDS_PER_MINUTE / dataRef.BPMChange) + dataRef.Timestamp;
    }

    public double GetSecondsPerTickAtTick(int tick)
    {
        var lastTickEvent = TempoEvents.GetPreviousTickEventInLane(tick, inclusive: true);

        return 1 / (double)Chart.Resolution * SECONDS_PER_MINUTE / TempoEvents[lastTickEvent].BPMChange;
    }

    // This may seem weird at first, but because the duration of a tick varies from tick to tick based on BPM changes,
    // to accurately convert a tick duration to seconds, you must subtract the end time from the start time.
    public double ConvertTickDurationToSeconds(int startTick, int endTick)
    {
        return ConvertTickTimeToSeconds(endTick) - ConvertTickTimeToSeconds(startTick);
    }

    public double ConvertTickDurationToSeconds(int startTick, ISustainable sustainData) => ConvertTickDurationToSeconds(startTick, startTick + sustainData.Sustain);

    #endregion

    #region Time Signature
    
    public BaseBeatline.BeatlineType CalculateBeatlineType(int beatlineTickTimePos, bool ignoreValidity = true)
    {
        // includes 0 at all times
        if (ignoreValidity && TimeSignatureEvents.Contains(beatlineTickTimePos)) return BaseBeatline.BeatlineType.barline;

        int lastTSTickTimePos = TimeSignatureEvents.GetPreviousTickEventInLane(beatlineTickTimePos);
        if (lastTSTickTimePos < 0) lastTSTickTimePos = 0;

        var tsDiff = beatlineTickTimePos - lastTSTickTimePos; // need absolute distance between the current tick and the origin of the TS event

        if (tsDiff % GetBarlineStep(lastTSTickTimePos) == 0) return BaseBeatline.BeatlineType.barline;
        else if (tsDiff % GetDivisionStep(lastTSTickTimePos) == 0) return BaseBeatline.BeatlineType.divisionLine;
        else if (tsDiff % GetHalfDivisionStep(lastTSTickTimePos) == 0) return BaseBeatline.BeatlineType.halfDivisionLine;
        return BaseBeatline.BeatlineType.none;
    }

    private float GetBarlineStep(int tsPos) => Chart.Resolution * (float)TimeSignatureEvents[tsPos].Numerator / (float)(TimeSignatureEvents[tsPos].Denominator / 4.0f);
    private float GetDivisionStep(int tsPos) => Chart.Resolution / (float)TimeSignatureEvents[tsPos].Denominator * 4;
    private float GetHalfDivisionStep(int tsPos) => Chart.Resolution / ((float)TimeSignatureEvents[tsPos].Denominator / 2);

    /// <summary>
    /// Calculate the last "1" of a bar from a tick-time timestamp.
    /// </summary>
    /// <param name="currentTick">The tick-time timestamp to evaluate from.</param>
    /// <returns>The tick-time timestamp of the last barline.</returns>
    public int GetLastBarline(int currentTick)
    {
        var ts = TimeSignatureEvents.GetPreviousTickEventInLane(currentTick);
        if (ts < 0) ts = 0;

        var tickDiff = currentTick - ts;
        var tickInterval = GetBarlineStep(ts);
        int numIntervals = (int)Math.Floor(tickDiff / tickInterval); // floor is to snap it back to the minimum interval (get LAST barline, not closest)

        return (int)(ts + numIntervals * tickInterval);
    }

    /// <summary>
    /// Calculate the next beatline to be generated from a specified tick-time timestamp.
    /// </summary>
    /// <param name="currentTick"></param>
    /// <returns>The tick-time timestamp of the next beatline event.</returns>
    public int GetNextBeatlineEvent(int currentTick)
    {
        var ts = TimeSignatureEvents.GetPreviousTickEventInLane(currentTick);
        if (ts < 0) ts = 0;

        var tickDiff = currentTick - ts;
        var tickInterval = GetHalfDivisionStep(ts);
        var numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        return (int)(ts + numIntervals * tickInterval);
    }

    public int GetPreviousBeatlineEvent(int currentTick)
    {
        var ts = TimeSignatureEvents.GetPreviousTickEventInLane(currentTick);
        if (ts < 0) ts = 0;

        var tickDiff = currentTick - ts;
        var tickInterval = GetHalfDivisionStep(ts);
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        return (int)(ts + (numIntervals * tickInterval - 2));
    }

    public int GetPreviousBeatlineEventExclusive(int currentTick)
    {
        currentTick--;
        var proposedPrevious = GetPreviousBeatlineEvent(currentTick);

        var middleTSEvent = TimeSignatureEvents.GetPreviousTickEventInLane(proposedPrevious);

        return middleTSEvent != TimeSignatureEvents.GetPreviousTickEventInLane(currentTick) ? middleTSEvent : proposedPrevious;
    }

    public int GetNextDivisionEvent(int currentTick)
    {
        var ts = TimeSignatureEvents.GetPreviousTickEventInLane(currentTick);
        if (ts < 0) ts = 0;

        var tickDiff = currentTick - ts;
        var tickInterval = GetDivisionStep(ts);
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

        return (int)(ts + numIntervals * tickInterval);
    }

    public int GetNextBeatlineEventExclusive(int currentTick)
    {
        currentTick++;
        var proposedNext = GetNextBeatlineEvent(currentTick);

        var middleTSEvent = TimeSignatureEvents.GetPreviousTickEventInLane(proposedNext);

        // edge case where a new TS event falls within the calculated next event and current tick
        // happens if a TS event is placed on a non-beatline - that new TS has to be the next barline
        // this is only something that applies during testing stage for a charter - still important tho
        if (middleTSEvent != TimeSignatureEvents.GetPreviousTickEventInLane(currentTick))
        {
            return middleTSEvent;
        }
        return proposedNext;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tick">Time signature event tick-timestamp</param>
    /// <returns>Is the time signature in proper alignment? <para>Aligned = The TS event falls on a "beat 1" of the last active TS event.</para></returns>
    /// <remarks>CH (and maybe YARG?) can't parse unaligned time signatures. All time signatures must be aligned.</remarks>
    public bool IsTimeSignatureEventValid(int tick)
    {
        // FIXME: Every time event is placed run this check for all future events and put alert on scrubber
        return CalculateBeatlineType(tick, ignoreValidity: false) == BaseBeatline.BeatlineType.barline;
    }

    public int ConvertBarsToTicks(int startTick, float bars)
    {
        var workingTick = startTick;
        var accumulatedTicks = 0;

        while (bars > 0)
        {
            var currentTSEventTimestamp = TimeSignatureEvents.GetPreviousTickEventInLane(workingTick, inclusive: true);
            var ticksPerBar = Mathf.FloorToInt(GetBarlineStep(currentTSEventTimestamp));
            
            var nextTSEventTimestamp = TimeSignatureEvents.GetNextTickEventInLane(workingTick);

            var barDurationCandidate = bars * ticksPerBar;
            if (nextTSEventTimestamp != LaneSet<TSData>.NO_TICK_EVENT && nextTSEventTimestamp < workingTick + barDurationCandidate)
            {
                var tickDistance = nextTSEventTimestamp - workingTick;
                accumulatedTicks += tickDistance;
                
                workingTick = nextTSEventTimestamp;

                // mmm algebra
                bars -= (tickDistance / (float)ticksPerBar);
            }
            else
            {
                accumulatedTicks += Mathf.FloorToInt(barDurationCandidate);
                bars = 0;
            }
        }

        return accumulatedTicks;
    }

    #endregion

    #region Selections

    public override string ConvertSelectionToString()
    {
        var bpmSelectionData = Lanes.bpmSelection.ExportNormalizedData();
        var tsSelectionData = Lanes.tsSelection.ExportNormalizedData();
        var stringIDs = new List<KeyValuePair<int, string>>();

        foreach (var item in bpmSelectionData)
        {
            stringIDs.Add(
                new KeyValuePair<int, string>(item.Key, item.Value.ToChartFormat(0)[0])
                );
            if (item.Value.Anchor)
            {
                stringIDs.Add(
                    new KeyValuePair<int, string>(item.Key, $"A {item.Value.Timestamp * MICROSECOND_CONVERSION}")
                    );
            }
        }
        foreach (var item in tsSelectionData)
        {
            stringIDs.Add(
                new KeyValuePair<int, string>(item.Key, item.Value.ToChartFormat(0)[0])
                );
        }

        stringIDs.Sort((a, b) => a.Key.CompareTo(b.Key));

        StringBuilder combinedIDs = new();

        foreach (var id in stringIDs)
        {
            combinedIDs.AppendLine($"\t{id.Key} = {id.Value}");
        }
        return combinedIDs.ToString();
    }

    #endregion

    #region Parsing

    protected override void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        HashSet<int> anchoredTicks = new(); // allows for versitility if A event comes before or after tempo event proper

        foreach (var entry in lines)
        {
            if (entry.Value.Contains(TEMPO_EVENT_INDICATOR))
            {
                var eventData = entry.Value;
                eventData = eventData.Replace($"{TEMPO_EVENT_INDICATOR} ", ""); // SPACE IS VERY IMPORTANT HERE

                if (!int.TryParse(eventData, out int bpmNoDecimal))
                {
                    Chart.Log($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid tempo entry.");
                    continue;
                }

                float bpmWithDecimal = bpmNoDecimal / BPM_FORMAT_CONVERSION;

                // timestamp will be calculated by the RecalculateTempoEventDictionary call following this
                TempoEvents[entry.Key] = new BPMData((float)Math.Round(bpmWithDecimal, 3), timestamp: 0, anchoredTicks.Contains(entry.Key));
            }
            else if (entry.Value.Contains(TIME_SIGNATURE_EVENT_INDICATOR))
            {
                var eventData = entry.Value;
                eventData = eventData.Replace($"{TIME_SIGNATURE_EVENT_INDICATOR} ", "");

                string[] tsParts = eventData.Split(" ");

                if (!int.TryParse(tsParts[0], out int numerator))
                {
                    Chart.Log($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature numerator.");
                    continue;
                }

                int denominator = DEFAULT_TS_DENOMINATOR;
                if (tsParts.Length == 2) // There is no space in the event value (only one number)
                {
                    if (!int.TryParse(tsParts[1], out int denominatorLog2))
                    {
                        Chart.Log($"{SYNC_TRACK_ERROR} [{entry.Key} = {entry.Value}]. Error type: Invalid time signature denominator.");
                        continue;
                    }
                    denominator = (int)Math.Pow(TS_POWER_CONVERSION_NUMBER, denominatorLog2);
                }

                TimeSignatureEvents[entry.Key] = new TSData(numerator, denominator);
            }
            else if (entry.Value.Contains(ANCHOR_IDENTIFIER))
            {
                anchoredTicks.Add(entry.Key);

                // if for some reason you need to add parsing for the microsecond value do it here
                // that is not here because a) penguin already works with and calculates the timestamps of every event
                // and b) if the microsecond value is parsed and it's not aligned with the Format calculations,
                // then what is penguin supposed to do? change the incoming BPM data? no
                // I think it has the microsecond value for programs that choose not to work with timestamps
                // (timestamps are easier to deal with in my opinion, even if an extra (minor) step is needed after every edit)
            }
        }

        foreach (var anchoredTick in anchoredTicks)
        {
            if (TempoEvents.Contains(anchoredTick))
            {
                TempoEvents[anchoredTick] = new BPMData(TempoEvents[anchoredTick].BPMChange, 0, true);
            }
        }

        RecalculateTempoEventDictionary();
    }

    #endregion
    
    #region Export

    public override List<string> ExportAllEvents()
    {
        var syncTrackStrings = ExportTempoEvents();
        syncTrackStrings.AddRange(ExportTimeSignatureEvents());
        var orderedEvents = syncTrackStrings.OrderBy(i => int.Parse(i.Split(" = ")[0])).ToList();
        return orderedEvents;
    }

    public List<string> ExportTempoEvents()
    {
        List<string> eventContainer = new(TempoEvents.Count);
        foreach (var @event in TempoEvents)
        {
            eventContainer.Add
                (
                    $"\t{@event.Key} = {@event.Value.ToChartFormat(0)}"
                );

            if (@event.Value.Anchor)
            {
                eventContainer.Add($"{@event.Key} = {ANCHOR_IDENTIFIER} {@event.Value.Timestamp * MICROSECOND_CONVERSION}");
            }
        }
        return eventContainer;
    }

    public List<string> ExportTimeSignatureEvents()
    {
        List<string> eventContainer = new(TimeSignatureEvents.Count);
        foreach (var @event in TimeSignatureEvents)
        {
            string output = $"\t{@event.Key} = {@event.Value.ToChartFormat(0)}";
            eventContainer.Add(output);
        }
        return eventContainer;
    }

    #endregion

    #region Not Implemented

    protected override void InternalSetSelectionToNewLane(int destinationLane)
    {
        throw new NotImplementedException("This instrument does not support setting selections to new lanes.");
    }

    #endregion

}

