using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class SyncTrackInstrument : IInstrument
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
    public const int MICROSECOND_CONVERSION = 1000000;
    private const float BPM_FORMAT_CONVERSION = 1000.0f;
    private const int TS_POWER_CONVERSION_NUMBER = 2;

    #endregion

    #region Data/Setup

    // Data types are fundamentally different, very hard to combine into one single Lanes object
    // Both are also structs because of their small (and repeatable) size.
    // Converting from IEventData to XData every time you want to get a statistic would be too much overhead. 
    public LaneSet<BPMData> TempoEvents { get; set; }
    public LaneSet<TSData> TimeSignatureEvents { get; set; }

    public SelectionSet<BPMData> bpmSelection;
    public SelectionSet<TSData> tsSelection;

    public ILaneData GetLaneData(int lane)
    {
        var laneAsOrientation = (LaneOrientation)lane;
        if (laneAsOrientation == LaneOrientation.bpm)
        {
            return TempoEvents;
        }
        return TimeSignatureEvents;
    }

    public ILaneData GetBarLaneData() => throw new NullReferenceException("SyncTrackInstrument does not have a bar lane, as it does not use traditional instrument lanes.");

    public ISelection GetLaneSelection(int lane)
    {
        var laneAsOrientation = (LaneOrientation)lane;
        if (laneAsOrientation == LaneOrientation.bpm)
        {
            return bpmSelection;
        }
        return tsSelection;
    }

    public bool IsNoteSelectionEmpty() => bpmSelection.Count == 0 && tsSelection.Count == 0;

    public SoloDataSet SoloData
    {
        get { throw new NotImplementedException("SyncTrack does not have solo events. If you are using the SoloEvent suite, it is not required."); }
        set { throw new NotImplementedException("SyncTrack does not have solo events. If you are using the SoloEvent suite, it is not required."); }
    }

    public InstrumentType InstrumentName { get; set; } = InstrumentType.synctrack;
    public DifficultyType Difficulty { get; set; } = DifficultyType.easy;
    public HeaderType InstrumentID => HeaderType.SyncTrack;

    public List<int> GetUniqueTickSet()
    {
        var hashSet = Chart.SyncTrackInstrument.TempoEvents.ExportData().Keys.ToHashSet();
        hashSet.UnionWith(TimeSignatureEvents.ExportData().Keys.ToHashSet());
        List<int> list = new(hashSet);
        list.Sort();
        return list;
    }

    public SyncTrackInstrument(LaneSet<BPMData> bpmEvents, LaneSet<TSData> tsEvents)
    {
        TempoEvents = bpmEvents;
        TimeSignatureEvents = tsEvents;
        bpmSelection = new(TempoEvents);
        tsSelection = new(TimeSignatureEvents);
        SetUpInputMap();
    }

    public SyncTrackInstrument(List<KeyValuePair<int, string>> fileData)
    {
        TempoEvents = new(protectedTicks: new() { 0 });
        TimeSignatureEvents = new(protectedTicks: new() { 0 });

        bpmSelection = new(TempoEvents);
        tsSelection = new(TimeSignatureEvents);

        AddChartFormattedEventsToInstrument(fileData);
        if (TempoEvents.Count == 0)
        {
            TempoEvents[0] = new(120, 0, false);
        }

        if (TimeSignatureEvents.Count == 0)
        {
            TimeSignatureEvents[0] = new(4, 4);
        }
    }

    private InputMap inputMap;
    public void SetUpInputMap() 
    {
        inputMap = new();
        inputMap.Enable();
        inputMap.Charting.YDrag.performed += x => 
        {
            // Let BPM labels do their thing undisturbed if applicable
            if (!Input.GetKey(KeyCode.LeftControl)) 
                MoveSelection(); 
        };

        inputMap.Charting.LMB.canceled += x => CompleteMove();

        inputMap.Charting.SelectAll.performed += x =>
        {
            bpmSelection.SelectAllInLane();
            tsSelection.SelectAllInLane();
        };

        inputMap.Charting.Delete.performed += x => DeleteSelection();
        inputMap.Charting.LMB.performed += x => CheckForSelectionClear();
    }

    public enum LaneOrientation
    {
        bpm = 0,
        timeSignature = 1
    }

    #endregion

    #region Movement

    private UniversalMoveData<BPMData> bpmMoveData = new();
    private UniversalMoveData<TSData> tsMoveData = new();

    /// <summary>
    /// Runs every frame when Drag input action is active. 
    /// </summary>
    private void MoveSelection()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;

        var currentMouseTick = SongTime.CalculateGridSnappedTick(Input.mousePosition.y / Screen.height);

        MoveLane(currentMouseTick, ref bpmMoveData, bpmSelection, TempoEvents);
        MoveLane(currentMouseTick, ref tsMoveData, tsSelection, TimeSignatureEvents);
    }

    private void MoveLane<T>(int currentMouseTick, ref UniversalMoveData<T> moveData, SelectionSet<T> selection, LaneSet<T> lane) where T : IEventData
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() && !moveData.inProgress)
        {
            return;
        }

        if (currentMouseTick == moveData.lastMouseTick) 
        {
            return;
        }

        if (!moveData.inProgress)
        {
            moveData = new UniversalMoveData<T>(
                    currentMouseTick,
                    lane,
                    selection
                );
            Chart.showPreviewers = false;
            return;
        }

        lane.OverwriteLaneDataWith(moveData.preMoveData[0]);

        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;

        var pasteDestination = moveData.firstSelectionTick + cursorMoveDifference;
        moveData.lastGhostStartTick = pasteDestination;

        var movingDataSet = moveData.OneDGetMoveData()[0];

        lane.OverwriteDataWithOffset(movingDataSet, pasteDestination);

        selection.ApplyScaledSelection(movingDataSet, moveData.lastGhostStartTick);

        moveData.lastMouseTick = currentMouseTick;

        if (typeof(T) == typeof(BPMData)) RecalculateTempoEventDictionary();

        Chart.SyncTrackInPlaceRefresh();
    }

    public void CompleteMove()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;

        Chart.showPreviewers = true;

        CompleteMove(ref bpmMoveData, bpmSelection);
        CompleteMove(ref tsMoveData, tsSelection);

        Chart.SyncTrackInPlaceRefresh();
    }

    public void CompleteMove<T>(ref UniversalMoveData<T> moveData, SelectionSet<T> selection) where T : IEventData
    {
        if (!moveData.inProgress) return;

        moveData = new();
    }

    #endregion

    #region Add/Delete

    private void DeleteSelection()
    {
        if (Chart.LoadedInstrument != this) return;

        if (bpmSelection.Count > 0)
        {
            TempoEvents.PopTicksFromSet(bpmSelection.ExportData());
            RecalculateTempoEventDictionary();
            bpmSelection.Clear();
        }

        if (tsSelection.Count > 0)
        {
            TimeSignatureEvents.PopTicksFromSet(tsSelection.ExportData());
            tsSelection.Clear();
        }

        Chart.SyncTrackInPlaceRefresh();
    }

    public void DeleteTickInLane(int tick, int lane)
    {
        if (lane == (int)LaneOrientation.bpm)
        {
            if (!TempoEvents.Contains(tick)) return;
            var poppedTick = TempoEvents.PopSingle(tick);
            if (poppedTick == null) return;

            RecalculateTempoEventDictionary();
        }

        if (lane == (int)LaneOrientation.timeSignature)
        {
            if (!TimeSignatureEvents.Contains(tick)) return;
            var poppedTick = TimeSignatureEvents.PopSingle(tick);
            if (poppedTick == null) return;
        }

        Chart.SyncTrackInPlaceRefresh();
    }

    public void DeleteAllEventsAtTick(int tick)
    {
        if (TempoEvents.Contains(tick)) TempoEvents.PopSingle(tick);
        if (TimeSignatureEvents.Contains(tick)) TempoEvents.PopSingle(tick);

        Chart.SyncTrackInPlaceRefresh();
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
    // NO EVIDENCE FOR INACCURACY AT THIS TIME
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
            var timestamp = tick == 0 ? new(bpmData.BPMChange, 0, bpmData.Anchor) : TempoEvents[tickEvents[i]];

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

    public double ConvertTickTimeToSeconds(int ticktime)
    {
        if (ticktime == 0) return 0;

        var lastTickEvent = TempoEvents.GetPreviousTickEventInLane(ticktime, inclusive: true);

        // Formula from .chart format specifications
        var dataRef = TempoEvents[lastTickEvent];
        return ((ticktime - lastTickEvent) / (double)Chart.Resolution * SECONDS_PER_MINUTE / dataRef.BPMChange) + dataRef.Timestamp;
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
        int numIntervals = (int)Math.Ceiling(tickDiff / tickInterval);

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

        if (middleTSEvent != TimeSignatureEvents.GetPreviousTickEventInLane(currentTick))
        {
            return middleTSEvent;
        }
        return proposedPrevious;
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

    public bool IsEventValid(int tick)
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

    public void CheckForSelectionClear()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || Chart.instance.SceneDetails.IsEventDataHit()) return;

        bpmSelection.Clear();
        tsSelection.Clear();
    }
    public void DeleteTicksInSelection()
    {
        bpmSelection.PopSelectedTicksFromLane();
        tsSelection.PopSelectedTicksFromLane();

        RecalculateTempoEventDictionary();
    }

    public void ClearAllSelections()
    {
        bpmSelection.Clear();
        tsSelection.Clear();
    }

    public bool NoteSelectionContains(int tick, int lane)
    {
        if ((LaneOrientation)lane == LaneOrientation.bpm)
        {
            return bpmSelection.Contains(tick);
        }
        else if ((LaneOrientation)lane == LaneOrientation.timeSignature)
        {
            return tsSelection.Contains(tick);
        }
        return false;
    }

    public void ShiftClickSelectLane(int start, int end, int lane)
    {
        var trackID = (LaneOrientation)lane;

        if (trackID == LaneOrientation.bpm)
        {
            bpmSelection.ShiftClickSelectInRange(start, end);
        }
        else
        {
            tsSelection.ShiftClickSelectInRange(start, end);
        }
    }

    public void ShiftClickSelect(int start, int end)
    {
        bpmSelection.Clear();
        tsSelection.Clear();

        bpmSelection.ShiftClickSelectInRange(start, end);
        tsSelection.ShiftClickSelectInRange(start, end);
    }

    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);

    public void ShiftClickSelect(int tick, bool temporary) => ShiftClickSelect(tick);

    public void ClearTickFromAllSelections(int tick)
    {
        bpmSelection.Remove(tick);
        tsSelection.Remove(tick);
    }

    public string ConvertSelectionToString()
    {
        var bpmSelectionData = bpmSelection.ExportNormalizedData();
        var tsSelectionData = tsSelection.ExportNormalizedData();
        var stringIDs = new List<KeyValuePair<int, string>>();

        foreach (var item in bpmSelectionData)
        {
            stringIDs.Add(
                new(item.Key, item.Value.ToChartFormat(0)[0])
                );
            if (item.Value.Anchor)
            {
                stringIDs.Add(
                    new(item.Key, $"A {item.Value.Timestamp * MICROSECOND_CONVERSION}")
                    );
            }
        }
        foreach (var item in tsSelectionData)
        {
            stringIDs.Add(
                new(item.Key, item.Value.ToChartFormat(0)[0])
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

    public int NoteSelectionCount
    {
        get
        {
            return bpmSelection.Count + tsSelection.Count;
        }
    }

    #endregion

    #region Parsing

    public void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        AddChartFormattedEventsToInstrument(Clipboard.ConvertToLineList(clipboardData, offset));
    }

    public void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
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
                TempoEvents[entry.Key] = new((float)Math.Round(bpmWithDecimal, 3), timestamp: 0, anchoredTicks.Contains(entry.Key));
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
                TempoEvents[anchoredTick] = new(TempoEvents[anchoredTick].BPMChange, 0, true);
            }
        }

        RecalculateTempoEventDictionary();
    }

    #endregion

    #region Unused
    public void ReleaseTemporaryTicks() { } // unneeded - no sustains lol

    #endregion

    #region Export

    public List<string> ExportAllEvents()
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

    public bool justMoved { get; set; }
}