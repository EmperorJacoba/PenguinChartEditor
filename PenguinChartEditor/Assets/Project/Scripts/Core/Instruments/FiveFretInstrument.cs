using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

public class FiveFretInstrument : IInstrument
{
    #region Constants 

    const string NOTE_INDICATOR = "N";
    const string SPECIAL_INDICATOR = "S";
    const string EVENT_INDICATOR = "E";
    const string DEPRECATED_HAND_INDICATOR = "H";
    const int IDENTIFIER_INDEX = 0;
    const int NOTE_IDENTIFIER_INDEX = 1;
    const int SUSTAIN_INDEX = 2;
    const string FORCED_SUBSTRING = "N 5 0";
    const string TAP_SUBSTRING = "N 6 0";
    const int EVENT_DATA_INDEX = 1;
    const int LAST_VALID_IDENTIFIER = 7;
    const int OPEN_IDENTIFIER = 7;
    const int STARPOWER_INDICATOR = 2;
    const string TAP_ID = "N 6 0";
    const string EXPLICIT_STRUM_ID = "N FS 0";
    const string EXPLICIT_HOPO_ID = "N FH 0";

    #endregion

    #region Data

    public Lanes<FiveFretNoteData> Lanes { get; set; }
    public SortedDictionary<int, SpecialData> SpecialEvents { get; set; }
    public SortedDictionary<int, LocalEventData> LocalEvents { get; set; }
    public InstrumentType InstrumentName { get; set; }
    public DifficultyType Difficulty { get; set; }

    InputMap inputMap;

    /// <summary>
    /// Corresponds to this lane's position in Lanes.
    /// </summary>
    public enum LaneOrientation
    {
        green = 0,
        red = 1,
        yellow = 2,
        blue = 3,
        orange = 4,
        open = 5
    }

    public List<int> UniqueTicks => Lanes.UniqueTicks;

    #endregion

    #region Constructor

    public FiveFretInstrument(
        Lanes<FiveFretNoteData> lanes,
        SortedDictionary<int, SpecialData> starpower,
        SortedDictionary<int, LocalEventData> localEvents,
        InstrumentType instrument,
        DifficultyType difficulty
        )
    {
        Lanes = lanes;
        SpecialEvents = starpower;
        LocalEvents = localEvents;
        InstrumentName = instrument;
        Difficulty = difficulty;

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneIndex = i;
            // add Lanes update needed
            // change to generic validateblic
            Lanes.GetLane(laneIndex).UpdatesNeededInRange += (startTick, endTick) =>
            {
                if (startTick == endTick) CheckForHopos((LaneOrientation)laneIndex, startTick);
                else CheckForHoposInRange(startTick, endTick);
            };
            Lanes.UpdatesNeededInRange += (startTick, endTick) =>
            {
                CheckForHoposInRange(startTick, endTick);
            };
        }
    }

    public void SetUpInputMap()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ForceTap.performed += x => ToggleTaps();
        inputMap.Charting.XYDrag.performed += x => MoveSelection();
        inputMap.Charting.LMB.canceled += x => CompleteMove();
        inputMap.Charting.Delete.performed += x => DeleteSelection();
        inputMap.Charting.SustainDrag.performed += x => SustainSelection();
        inputMap.Charting.RMB.canceled += x => CompleteSustain();
        inputMap.Charting.LMB.performed += x => CheckForSelectionClear();
        inputMap.Charting.SelectAll.performed += x => Lanes.SelectAll();
    }

    #endregion

    #region Moving

    UniversalMoveData<FiveFretNoteData> moveData = new();
    public bool justMoved { get; set; } = false;

    void MoveSelection()
    {
        if (this != Chart.LoadedInstrument) return;

        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() && !moveData.inProgress) return;
        bool tickMovement = false;
        bool laneMovement = false;

        var currentMouseTick = SongTime.CalculateGridSnappedTick(Chart.instance.SceneDetails.GetCursorHighwayProportion());
        var currentMouseLane = Chart.instance.SceneDetails.MatchXCoordinateToLane(Chart.instance.SceneDetails.GetCursorHighwayPosition().x);

        if (currentMouseTick != moveData.lastMouseTick)
        {
            moveData.lastMouseTick = currentMouseTick;
            tickMovement = true;
        }
        if (currentMouseLane != moveData.lastLane)
        {
            moveData.lastLane = currentMouseLane;
            laneMovement = true;
        }

        if (!moveData.inProgress && (tickMovement || laneMovement))
        {
            // optimize call
            moveData = new(
                currentMouseTick,
                currentLane: currentMouseLane,
                Lanes.ExportData(),
                Lanes.ExportNormalizedSelection(),
                Lanes.GetFirstSelectionTick()
                );
            Chart.showPreviewers = false;
            return;
        }
        if (!(tickMovement || laneMovement)) return;

        Lanes.SetLaneData(moveData.preMoveData);

        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;

        var movingDataSet = moveData.GetMoveData(currentMouseLane - moveData.firstLane);

        var pasteDestination = moveData.firstSelectionTick + cursorMoveDifference;
        moveData.lastGhostStartTick = pasteDestination;
        Lanes.OverwriteLaneDataWithOffset(movingDataSet, pasteDestination);

        Lanes.ApplyScaledSelection(movingDataSet, moveData.lastGhostStartTick);

        CheckForHoposInRange(moveData.lastGhostStartTick, moveData.lastGhostEndTick);

        Chart.Refresh();
    }

    void CompleteMove()
    {
        if (this != Chart.LoadedInstrument) return;

        Chart.showPreviewers = true;
        if (!moveData.inProgress) return;

        var movingDataSet = moveData.GetMoveData(moveData.lastLane - moveData.firstLane);
        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            if (movingDataSet[i].Count > 0)
            {
                var endTick = movingDataSet[i].Keys.Max() + moveData.lastGhostStartTick;
                var startTick = movingDataSet[i].Keys.Min() + moveData.lastGhostStartTick;

                ValidateSustainsInRange(startTick, endTick);
            }
        }

        moveData = new();
    }

    #endregion

    #region Add/Delete

    void DeleteSelection()
    {
        if (Chart.LoadedInstrument != this) return;

        if (TotalSelectionCount == 0) return;

        var totalSelection = Lanes.GetTotalSelection();
        Lanes.DeleteAllTicksInSelection();

        CheckForHoposInRange(totalSelection.Min(), totalSelection.Max());

        Lanes.ClearAllSelections();

        Chart.Refresh();
    }

    public void DeleteTick(int tick, int lane)
    {
        var laneReference = Lanes.GetLane(lane);
        if (!laneReference.Contains(tick)) return;

        var poppedTick = laneReference.PopSingle(tick);
        if (poppedTick == null) return; // future proofing in case a protected tick is ever needed for FFN

        Chart.Refresh();
    }

    #endregion

    #region Selections

    /// <summary>
    /// Set to true whenever a move concluded, set to false before an early return when a selection check happens
    /// Since OnPointerUp (and then CalculateSelectionStatus()) happens right after
    /// the move action (as both fire at the same time), the restored move selection is
    /// overwritten by the selection check in OnPointerUp.
    /// Thus, the selection check immediately after a move is invalid (which is what this represents)
    /// </summary>
    public bool disableNextSelectionCheck = false;

    public int TotalSelectionCount
    {
        get
        {
            var sum = 0;
            for (int i = 0; i < Lanes.Count; i++)
            {
                sum += Lanes.GetLaneSelection(i).Count;
            }
            return sum;
        }
    }

    public void ClearAllSelections() => Lanes.ClearAllSelections();

    public void DeleteTicksInSelection()
    {
        Lanes.DeleteAllTicksInSelection();
    }

    public void ShiftClickSelect(int tick, bool temporary)
    {
        Lanes.TempSustainTicks.Add(tick);
        ShiftClickSelect(tick);
    }

    public void ShiftClickSelect(int start, int end)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            Lanes.GetLaneSelection(i).ShiftClickSelectInRange(start, end);
        }
    }
    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);

    public void RemoveTickFromAllSelections(int tick)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            Lanes.GetLaneSelection(i).Remove(tick);
        }
    }

    public void CheckForSelectionClear()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || Chart.instance.SceneDetails.IsEventDataHit()) return;

        Lanes.ClearAllSelections();
    }

    public string ConvertSelectionToString()
    {
        if (Lanes.GetTotalSelection().Count == 0) return "";
        var stringIDs = new List<KeyValuePair<int, string>>();
        var zeroTick = Lanes.GetFirstSelectionTick();

        HashSet<int> tapTicks = new();
        HashSet<int> strumTicks = new();
        HashSet<int> hopoTicks = new();

        // add functionality here to allow N 5 forced pasting instead
        for (int i = 0; i < Lanes.Count; i++)
        {
            var selectionData = Lanes.GetLaneSelection(i).ExportNormalizedData(zeroTick);
            foreach (var note in selectionData)
            {
                stringIDs.Add(
                    new(note.Key, note.Value.ToChartFormat(i))
                    );

                if (!note.Value.Default)
                {
                    switch (note.Value.Flag)
                    {
                        case FiveFretNoteData.FlagType.hopo:
                            hopoTicks.Add(note.Key);
                            break;
                        case FiveFretNoteData.FlagType.strum:
                            strumTicks.Add(note.Key);
                            break;
                    }
                }
                if (note.Value.Flag == FiveFretNoteData.FlagType.tap) tapTicks.Add(note.Key);
            }
        }

        foreach (var tick in tapTicks)
        {
            stringIDs.Add(
                new(tick, TAP_ID)
                );
        }

        foreach (var tick in strumTicks)
        {
            stringIDs.Add(
                new(tick, EXPLICIT_STRUM_ID)
                );
        }

        foreach (var tick in hopoTicks)
        {
            stringIDs.Add(
                new(tick, EXPLICIT_HOPO_ID)
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

    public void SetSelectionToNewLane(LaneOrientation lane)
    {
        var currentSelection = Lanes.GetTotalSelectionByLane();
        var targetLane = Lanes.GetLane((int)lane);
        var targetLaneSelection = Lanes.GetLaneSelection((int)lane);

        for (int i = 0; i < Lanes.Count; i++)
        {
            if (i == (int)lane) continue;

            var laneSelection = currentSelection[i];
            if (laneSelection.Count == 0) continue;

            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in laneSelection)
            {
                targetLane[selectedNote] = changingLane[selectedNote];
                changingLane.Remove(selectedNote);
                targetLaneSelection.Add(selectedNote);
            }
        }

        Chart.Refresh();
    }

    public void NaturalizeSelection()
    {
        var currentSelection = Lanes.GetTotalSelectionByLane();
        var totalSelectionSet = Lanes.GetTotalSelection();

        if (totalSelectionSet.Count == 0) return;

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneSelection = currentSelection[i];
            if (laneSelection.Count == 0) continue;

            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in laneSelection)
            {
                var tickData = changingLane[selectedNote];

                // strum will be overwritten by the check at the end of this function
                // this is explicitly done to get rid of tap flags if they exist within the selection
                changingLane[selectedNote] = new FiveFretNoteData(tickData.Sustain, FiveFretNoteData.FlagType.strum, true);
            }
        }

        // use the range function b/c this is worlds
        // faster than checking every individual selection note
        // also ignores non-default notes and taps,
        // so the unselected notes won't be affected by this
        // (or will have a corrected calculation on the
        // off-chance that it was missed somewhere down the line)
        CheckForHoposInRange(totalSelectionSet.Min(), totalSelectionSet.Max());

        Chart.Refresh();
    }

    public void SetSelectionToFlag(FiveFretNoteData.FlagType flag)
    {
        var currentSelection = Lanes.GetTotalSelectionByLane();

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneSelection = currentSelection[i];
            if (laneSelection.Count == 0) continue;

            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in laneSelection)
            {
                var tickData = changingLane[selectedNote];
                changingLane[selectedNote] = new FiveFretNoteData(tickData.Sustain, flag, false);
            }
        }

        Chart.Refresh();
    }

    public void SetSelectionSustain(int ticks)
    {
        var currentSelection = Lanes.GetTotalSelectionByLane();

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneSelection = currentSelection[i];
            if (laneSelection.Count == 0) continue;

            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in laneSelection)
            {
                UpdateSustain(selectedNote, (LaneOrientation)i, ticks);
            }
        }

        Chart.Refresh();
    }

    public void SetEqualSpacing()
    {
        var currentSelection = Lanes.GetTotalSelectionByLane();
        var totalSelectionSet = Lanes.GetTotalSelection().ToList();
        totalSelectionSet.Sort();

        var firstTick = totalSelectionSet.Min();
        var lastTick = totalSelectionSet.Max();
        var tickCoverage = lastTick - firstTick;
        var evenSpacingDistance = tickCoverage / (totalSelectionSet.Count-1);

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneSelection = currentSelection[i];
            if (laneSelection.Count == 0) continue;

            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in new HashSet<int>(laneSelection))
            {
                var tickData = changingLane.PopSingle(selectedNote);
                if (tickData == null) continue;

                var index = totalSelectionSet.BinarySearch(selectedNote);
                var equalSpacingTick = (index * evenSpacingDistance) + firstTick;

                changingLane[equalSpacingTick] = tickData.First().Value;
                laneSelection.Add(equalSpacingTick);
            }
        }
        CheckForHoposInRange(firstTick, lastTick);
        ValidateSustainsInRange(firstTick, lastTick);

        Chart.Refresh();
    }

    #endregion

    #region Flag Changes
    void ChangeTickFlag(int targetTick, int previousTick, FiveFretNoteData.FlagType flag)
    {
        bool isLastTickChord = Lanes.IsTickChord(previousTick);
        bool isCurrentTickChord = Lanes.IsTickChord(targetTick);
        bool settingToTap = flag == FiveFretNoteData.FlagType.tap;

        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            if (!lane.Contains(targetTick)) continue;
            if (!lane[targetTick].Default) break;

            if (((!isLastTickChord && lane.Contains(previousTick)) || isCurrentTickChord) && !settingToTap)
            {
                flag = FiveFretNoteData.FlagType.strum;
            }

            if (lane[targetTick].Flag != flag)
            {
                lane[targetTick] = lane[targetTick].ExportWithNewFlag(flag);
            }
        }
    }

    #endregion

    #region Sustains

    // true = start sustain editing from the bottom of the note (sustain = 0)
    // use when editing sustains from the note (root of sustain)
    // false = start sustain editing from mouse cursor/current sustain positioning
    // use when editing sustain from the sustain tail 
    public static bool resetSustains = true;

    SustainData<FiveFretNoteData> sustainData = new();

    public void SustainSelection()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsEditAllowed()) return;

        // Early return if attempting to start an edit while over an overlay element
        // Allows edit to start only if interacting with main content
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() && !sustainData.sustainInProgress) return;

        var currentMouseTick = GetCurrentMouseTick();
        if (currentMouseTick == int.MinValue) return;

        // early return if no changes to mouse's grid snap
        if (currentMouseTick == sustainData.lastMouseTick) return;

        if (!sustainData.sustainInProgress)
        {
            // directly access parent lane here to avoid reassigning the local shortcut variable
            sustainData = new(Lanes.GetTotalSelectionByLane(), currentMouseTick);
            return;
        }

        for (int i = 0; i < Lanes.Count; i++)
        {
            var laneTicks = sustainData.sustainingTicks[i];
            var lane = Lanes.GetLane(i);

            foreach (var tick in laneTicks)
            {
                var newSustain = currentMouseTick - tick;

                // drag behind the note to max out sustain - cool feature from moonscraper
                // -CurrentDivison is easy arbitrary value for when to max out - so that there is a buffer for users to remove sustain entirely
                // SongLengthTicks will get clamped to max sustain length
                if (newSustain < -DivisionChanger.CurrentDivision) newSustain = SongTime.SongLengthTicks;

                if (lane.ContainsKey(tick))
                {
                    UpdateSustain(tick, (LaneOrientation)i, newSustain);
                }
            }
        }

        Chart.Refresh();
        sustainData.lastMouseTick = currentMouseTick;
    }

    public void CompleteSustain()
    {
        // parameterless new() = flag as empty 
        sustainData = new();
        Chart.Refresh();
    }

    public static int GetCurrentMouseTick()
    {
        var newHighwayPercent = Chart.instance.SceneDetails.GetCursorHighwayProportion();

        // 0 is very unlikely as an actual position (as 0 is at the very bottom of the TRACK, which should be outside the screen in most cases)
        // but is returned if cursor is outside track
        // min value serves as an easy exit check in case the cursor is outside the highway
        if (newHighwayPercent == 0) return int.MinValue;

        return SongTime.CalculateGridSnappedTick(newHighwayPercent);
    }

    public void ShiftClickSustainClamp(int tick, int tickLength)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            if (Lanes.GetLane(i).ContainsKey(tick))
            {
                Lanes.GetLane(i)[tick] = new(tickLength, Lanes.GetLane(i)[tick].Flag, Lanes.GetLane(i)[tick].Default);
            }
        }
    }

    public void ReleaseTemporaryTicks()
    {
        Lanes.TempSustainTicks.Clear();
    }

    public void ValidateSustain(int tick, LaneOrientation lane)
    {
        if (UserSettings.ExtSustains)
        {
            var extendedLaneRef = Lanes.GetLane((int)lane);
            if (!extendedLaneRef.Contains(tick)) return;

            UpdateSustain(tick, lane, extendedLaneRef[tick].Sustain);
        }
        else
        {
            var smallestSustain = int.MaxValue;

            for (int i = 0; i < Lanes.Count; i++)
            {
                var laneRef = Lanes.GetLane(i);
                if (!laneRef.Contains(tick)) continue;

                if (laneRef[tick].Sustain < smallestSustain) smallestSustain = laneRef[tick].Sustain;
            }

            UpdateSustain(tick, lane, smallestSustain);
        }
    }

    public void UpdateSustain(int tick, LaneOrientation lane, int newSustain)
    {
        // clamp based on this lane only (ignore other lane overlap)
        if (UserSettings.ExtSustains)
        {
            var currentLane = Lanes.GetLane((int)lane);
            if (!currentLane.Contains(tick)) return;

            currentLane[tick] = currentLane[tick].ExportWithNewSustain(
                CalculateSustainClamp(newSustain, tick, currentLane.GetNextTickEventInLane(tick))
                );
        }
        // clamp based on ALL lanes
        else
        {
            var ticks = Lanes.GetTickEventBounds(tick);
            var calculatedCurrentSustain = CalculateSustainClamp(newSustain, tick, ticks.next);

            for (int i = 0; i < Lanes.Count; i++)
            {
                var iteratorLane = Lanes.GetLane(i);

                if (iteratorLane.Contains(tick))
                {
                    iteratorLane[tick] = iteratorLane[tick].ExportWithNewSustain(calculatedCurrentSustain);
                }
            }
        }
    }

    public void ValidateSustainsInRange(int startTick, int endTick)
    {
        var uniqueTicks = Lanes.UniqueTicks;
        var uniqueTicksInRange = uniqueTicks.Where(tick => tick >= startTick && tick <= endTick).ToList();

        if (UserSettings.ExtSustains)
        {
            for (int i = 0; i < Lanes.Count; i++)
            {
                var currentLane = Lanes.GetLane(i);
                ClampSustainsBefore(startTick, (LaneOrientation)i);

                for (int index = 0; index < uniqueTicksInRange.Count(); index++)
                {
                    int tick = uniqueTicksInRange[index];
                    if (currentLane.Contains(tick)) ValidateSustain(uniqueTicksInRange[index], (LaneOrientation)i);
                }
            }
        }
        else
        {
            // lane orientation is irrelevant, just pass in anything
            ClampSustainsBefore(startTick, LaneOrientation.green);

            for (int index = 0; index < uniqueTicksInRange.Count(); index++)
            {
                int tick = uniqueTicksInRange[index];
                ValidateSustain(uniqueTicksInRange[index], LaneOrientation.green);
            }
        }

        Chart.Refresh();
    }

    public void ClampSustainsBefore(int tick, LaneOrientation lane)
    {
        if (UserSettings.ExtSustains)
        {
            ClampLaneEventsBefore(tick, lane);
            return;
        }

        for (int i = 0; i < Lanes.Count; i++)
        {
            ClampLaneEventsBefore(tick, (LaneOrientation)i);
        }
    }

    void ClampLaneEventsBefore(int tick, LaneOrientation lane)
    {
        var currentLane = Lanes.GetLane((int)lane);

        var clampTargetTick = currentLane.GetPreviousTickEventInLane(tick);
        if (clampTargetTick == LaneSet<FiveFretNoteData>.NO_TICK_EVENT) return;

        var data = currentLane[clampTargetTick];
        currentLane[clampTargetTick] = data.ExportWithNewSustain(
            CalculateSustainClamp(data.Sustain, clampTargetTick, tick)
            );
    }

    public int CalculateSustainClamp(int sustainLength, int tick, LaneOrientation lane)
    {
        if (!UserSettings.ExtSustains)
        {
            return CalculateSustainClamp(sustainLength, tick, Lanes.GetTickEventBounds(tick).next);
        }
        else
        {
            return CalculateSustainClamp(sustainLength, tick, Lanes.GetLane((int)lane).GetNextTickEventInLane(tick));
        }
    }

    public int CalculateSustainClamp(int sustainLength, int tick, int nextTick)
    {
        int clampedSustain = sustainLength;
        if (nextTick != LaneSet<FiveFretNoteData>.NO_TICK_EVENT)
        {
            if (sustainLength + tick >= nextTick - UserSettings.SustainGapTicks)
            {
                clampedSustain = (nextTick - tick) - UserSettings.SustainGapTicks;
            }
        }
        else
        {
            if (sustainLength + tick >= SongTime.SongLengthTicks)
            {
                clampedSustain = (SongTime.SongLengthTicks - tick); // does sustain gap apply to end of song? 🤔
            }
        }
        var sustainLengthMS = Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick + clampedSustain) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick);
        return sustainLengthMS < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ? 0 : clampedSustain;
    }

    #endregion

    #region HOPOs

    public void CheckForHopos(LaneOrientation lane, int changedTick)
    {
        var activeLane = Lanes.GetLane((int)lane);

        bool nextTickHopo = false;
        bool currentTickHopo = false;
        bool changedTickExists = Lanes.AnyLaneContainsTick(changedTick);
        bool changedTickChord = changedTickExists ? Lanes.IsTickChord(changedTick) : false; // optimize?

        var ticks = Lanes.GetTickEventBounds(changedTick); // biggest bottleneck here btw

        if (ticks.next != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
            ticks.next - changedTick < Chart.hopoCutoff) nextTickHopo = true;

        if (ticks.prev != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
            changedTick - ticks.prev < Chart.hopoCutoff) currentTickHopo = true;

        if (activeLane.Contains(changedTick))
        {
            var parameterLaneTickData = activeLane[changedTick];
            if (!parameterLaneTickData.Default ||
                parameterLaneTickData.Flag == FiveFretNoteData.FlagType.tap)
            {
                currentTickHopo = false;
            }
        }

        var flag = currentTickHopo ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;
        ChangeTickFlag(changedTick, ticks.prev, flag);

        if (IsTickTap(ticks.next)) return;
        var nextFlag = nextTickHopo && changedTickExists ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

        ChangeTickFlag(ticks.next, changedTick, nextFlag);
    }

    public void CheckForHoposInRange(int startTick, int endTick)
    {
        var uniqueTicks = Lanes.UniqueTicks;

        int startIndex = uniqueTicks.BinarySearch(startTick);

        if (startIndex < 0)
        {
            startIndex = ~startIndex - 1;
        }

        int endIndex = uniqueTicks.BinarySearch(endTick);
        if (endIndex < 0)
        {
            endIndex = ~endIndex + 1;
        }
        if (endIndex >= uniqueTicks.Count) endIndex = uniqueTicks.Count - 1;

        for (int i = startIndex; i <= endIndex; i++)
        {
            if (i < 0 || i >= uniqueTicks.Count) continue;
            var currentTick = uniqueTicks[i];

            var prevTick = i != 0 ? uniqueTicks[i - 1] : -Chart.hopoCutoff;

            var flag = (currentTick - prevTick < Chart.hopoCutoff) && !Lanes.IsTickChord(currentTick) ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

            ChangeTickFlag(currentTick, prevTick, flag);
        }
    }

    public bool PreviewTickHopo(LaneOrientation lane, int tick)
    {
        var ticks = Lanes.GetTickEventBounds(tick);

        if (ticks.prev != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
            tick - ticks.prev < Chart.hopoCutoff &&
            (Lanes.GetTickCountAtTick(tick) == 0 && !Lanes.GetLane((int)lane).Contains(ticks.prev))
            ) return true;

        return false;
    }

    #endregion

    #region Taps

    public void ToggleTaps()
    {
        if (Chart.LoadedInstrument != this) return;

        var allTicksSelected = Lanes.GetTotalSelection();

        bool toggleToTaps = true;
        foreach (var tick in allTicksSelected)
        {
            if (IsTickTap(tick)) toggleToTaps = false;
        }

        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            foreach (var tick in allTicksSelected)
            {
                if (!lane.Contains(tick)) continue;

                lane[tick] = toggleToTaps ? lane[tick].ExportWithNewFlag(FiveFretNoteData.FlagType.tap) : lane[tick].ExportWithNewFlag(FiveFretNoteData.FlagType.hopo);
            }
        }

        if (!toggleToTaps) CheckForHoposInRange(allTicksSelected.Min(), allTicksSelected.Max());

        Chart.Refresh();
    }

    public bool IsTickTap(int tick)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            if (!lane.Contains(tick)) continue;

            if (lane[tick].Flag == FiveFretNoteData.FlagType.tap)
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Import

    // needs to clear out zone between start & end point of added events
    public void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        HashSet<int> uniqueTicks = lines.Select(item => item.Key).ToHashSet();

        if (uniqueTicks.Count == 0) return;
        Lanes.PopDataInRange(uniqueTicks.Min(), uniqueTicks.Max());

        foreach (var uniqueTick in uniqueTicks)
        {
            var eventsAtTick = lines.Where(item => item.Key == uniqueTick).Select(item => item.Value).ToList();

            // we accept both data ripped straight from a .chart file
            // or special penguin modifiers
            // penguinHopo and penguinStrum correspond to FH (forced hopo) and FS (forced strum) events
            // this is because Penguin does not treat notes as forced/unforced
            // they are nondefault or default
            // meaning they either stay the way they are no matter what happens to the chart or don't
            // (except in some cases, like if it is the first tick in a track)
            bool tapModifier = false;
            bool forcedModifier = false;
            bool penguinHopo = false;
            bool penguinStrum = false;

            foreach (var identifier in new List<string>(eventsAtTick))
            {
                if (identifier.Contains($"{FORCED_SUBSTRING}"))
                {
                    forcedModifier = true;
                    eventsAtTick.Remove(identifier);
                }

                if (identifier.Contains($"{TAP_SUBSTRING}"))
                {
                    tapModifier = true;
                    eventsAtTick.Remove(identifier);
                }

                if (identifier.Contains($"{EXPLICIT_HOPO_ID}"))
                {
                    penguinHopo = true;
                    eventsAtTick.Remove(identifier);
                }

                if (identifier.Contains($"{EXPLICIT_STRUM_ID}"))
                {
                    penguinStrum = true;
                    eventsAtTick.Remove(identifier);
                }
            }

            var noteCount = 0;
            for (int i = 0; i < eventsAtTick.Count; i++)
            {
                if (eventsAtTick[i].Contains("N")) noteCount++;
            }

            int noteIdentifier;
            int sustain;
            foreach (var @event in eventsAtTick)
            {
                var values = @event.Split(' ');
                switch (values[IDENTIFIER_INDEX])
                {
                    case NOTE_INDICATOR:

                        if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
                        {
                            Chart.Log($"Invalid note identifier for {InstrumentName} @ tick {uniqueTick}: {values[NOTE_IDENTIFIER_INDEX]}");
                            continue;
                        }

                        if (noteIdentifier > LAST_VALID_IDENTIFIER) continue;

                        if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                        {
                            Chart.Log($"Invalid sustain for {InstrumentName} @ tick {uniqueTick}: {values[SUSTAIN_INDEX]}");
                            continue;
                        }

                        LaneOrientation lane;
                        if (noteIdentifier != OPEN_IDENTIFIER)
                            lane = (LaneOrientation)noteIdentifier;
                        else lane = LaneOrientation.open; // open identifier is not the same as lane orientation (index of lane dictionary)

                        bool defaultOrientation = true; // equivilent to forced
                        FiveFretNoteData.FlagType flagType = FiveFretNoteData.FlagType.strum;
                        if (tapModifier)
                        {
                            flagType = FiveFretNoteData.FlagType.tap; // tap overrides any hopo/forcing logic
                        }
                        else
                        {
                            if (penguinHopo)
                            {
                                flagType = FiveFretNoteData.FlagType.hopo;
                                defaultOrientation = false;
                            }

                            if (penguinStrum)
                            {
                                flagType = FiveFretNoteData.FlagType.strum;
                                defaultOrientation = false;
                            }

                            if (forcedModifier)
                            {
                                flagType = PreviewTickHopo(lane, uniqueTick) ? FiveFretNoteData.FlagType.strum : FiveFretNoteData.FlagType.hopo;
                                defaultOrientation = false;
                            }
                        }

                        // default to strum, will be recalculated later
                        var noteData = new FiveFretNoteData(sustain, flagType, defaultOrientation);

                        Lanes.GetLane((int)lane)[uniqueTick] = noteData;

                        break;
                    case SPECIAL_INDICATOR:

                        if (!int.TryParse(values[NOTE_IDENTIFIER_INDEX], out noteIdentifier))
                        {
                            Chart.Log($"Invalid special identifier for {InstrumentName} @ tick {uniqueTick}: {values[NOTE_IDENTIFIER_INDEX]}");
                            break;
                        }

                        if (!int.TryParse(values[SUSTAIN_INDEX], out sustain))
                        {
                            Chart.Log($"Invalid sustain for {InstrumentName} @ tick {uniqueTick}: {values[SUSTAIN_INDEX]}");
                            break;
                        }

                        if (noteIdentifier != STARPOWER_INDICATOR) break; // should only have starpower indicator, no fills or anything

                        SpecialEvents[uniqueTick] = new SpecialData(sustain, SpecialData.EventType.starpower);

                        break;
                    case EVENT_INDICATOR:
                        if (!Enum.TryParse(typeof(LocalEventData.EventType), values[EVENT_DATA_INDEX], true, out var localEvent))
                        {
                            Chart.Log($"Error at {uniqueTick}: Unsupported event type: {values[EVENT_DATA_INDEX]}");
                            break;
                        }

                        LocalEvents[uniqueTick] = new LocalEventData((LocalEventData.EventType)localEvent);

                        break;
                    case DEPRECATED_HAND_INDICATOR:
                        continue;
                }
            }
        }

        CheckForHoposInRange(uniqueTicks.Min(), uniqueTicks.Max());
    }

    public void AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        var lines = new List<KeyValuePair<int, string>>();
        var clipboardAsLines = clipboardData.Split("\n");

        for (int i = 0; i < clipboardAsLines.Length; i++)
        {
            var line = clipboardAsLines[i];

            if (line.Trim() == "") continue;
            var parts = line.Split(" = ", 2);

            if (!int.TryParse(parts[0].Trim(), out int tick))
            {
                Chart.Log(@$"Problem parsing event {line}");
                continue;
            }

            if (i == 0)
            {
                offset -= tick;
            }

            lines.Add(new(tick + offset, parts[1]));
        }

        AddChartFormattedEventsToInstrument(lines);
    }

    #endregion

    #region Export

    // currently only supports N events, need support for E and S
    // also needs logic for when and where to place forced/tap identifiers (data in struct is not enough - flag is LITERAL value, forced is the toggle between default and not behavior)
    // throw away sustains that are too small (ms < user settings constant) (add setting to do extra validation, or do this when validators fail)
    public List<string> ExportAllEvents()
    {
        List<string> notes = new();
        for (int i = 0; i < Lanes.Count; i++)
        {
            foreach (var note in Lanes.GetLane(i))
            {
                string value = $"\t{note.Key} = {note.Value.ToChartFormat(i)}";
                notes.Add(value);
            }
        }

        var orderedStrings = notes.OrderBy(i => int.Parse(i.Split(" = ")[0])).ToList();
        return orderedStrings;
    }

    #endregion
}