using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class FiveFretInstrument : BaseSustainableInstrument<FiveFretNoteData>
{
    #region Constants

    private const string NOTE_INDICATOR = "N";
    private const string EVENT_INDICATOR = "E";
    private const string DEPRECATED_HAND_INDICATOR = "H";
    private const int IDENTIFIER_INDEX = 0;
    private const int NOTE_IDENTIFIER_INDEX = 1;
    private const int SUSTAIN_INDEX = 2;
    private const string FORCED_SUBSTRING = "N 5 0";
    private const string TAP_SUBSTRING = "N 6 0";
    private const int EVENT_DATA_INDEX = 1;
    private const int LAST_VALID_IDENTIFIER = 7;
    private const string TAP_ID = "N 6 0";
    private const string EXPLICIT_STRUM_ID = "N FS 0";
    private const string EXPLICIT_HOPO_ID = "N FH 0";

    #endregion

    #region Data Access

    protected override IMultiLaneController LaneController => Lanes;

    private Lanes<FiveFretNoteData> Lanes { get; set; }
    public override ILaneData GetBarLaneData() => GetLaneData(LaneOrientation.open);
    public LaneSet<FiveFretNoteData> GetLaneData(LaneOrientation lane) => Lanes.GetLane((int)lane);

    public SelectionSet<FiveFretNoteData> GetLaneSelection(LaneOrientation lane) => Lanes.GetLaneSelection((int)lane);
    
    #endregion

    #region LaneOrientation

    /// <summary>
    /// Corresponds to this lane's ID in Lanes.
    /// </summary>

    public enum LaneOrientation
    {
        open = 0,
        green = 1,
        red = 2,
        yellow = 3,
        blue = 4,
        orange = 5,
    }

    private LinkedList<int> laneOrdering = new(Enumerable.Range(0, 6));

    public static int MatchLaneOrientationToChartID(LaneOrientation lane)
    {
        return lane switch
        {
            LaneOrientation.open => 7,
            LaneOrientation.green => 0,
            LaneOrientation.red => 1,
            LaneOrientation.yellow => 2,
            LaneOrientation.blue => 3,
            LaneOrientation.orange => 4,
            _ => throw new ArgumentNullException($"{lane} is not one of the LaneOrientation options.")
        };
    }

    public static LaneOrientation MatchChartIDToLaneOrientation(int chartID)
    {
        return chartID switch
        {
            0 => LaneOrientation.green,
            1 => LaneOrientation.red,
            2 => LaneOrientation.yellow,
            3 => LaneOrientation.blue,
            4 => LaneOrientation.orange,
            5 => throw new ArgumentException($"Tried to read forced modifier (id: 5) as a note."),
            6 => throw new ArgumentException($"Tried to read tap modifier (id: 6) as a note."),
            7 => LaneOrientation.open,
            _ => throw new ArgumentException($"Passed in note ID (id : {chartID}) is unknown.")
        };
    }

    #endregion

    #region Constructor

    public FiveFretInstrument(HeaderType instrumentID, List<KeyValuePair<int, string>> instrumentInfo)
    {
        Lanes = new Lanes<FiveFretNoteData>(6);
        sustainer = new SustainHelper<FiveFretNoteData>(this, Lanes, true);

        InstrumentName = InstrumentMetadata.GetInstrumentType(instrumentID);
        Difficulty = InstrumentMetadata.GetDifficulty(instrumentID);

        AddChartFormattedEventsToInstrument(instrumentInfo);

        foreach (var lane in Lanes.LaneKeys)
        {
            // add Lanes update needed
            // change to generic validateblic
            Lanes.GetLane(lane).UpdatesNeededInRange += (startTick, endTick) =>
            {
                if (startTick == endTick) CheckForHopos(startTick);
                else CheckForHoposInRange(startTick, endTick);
            };
            Lanes.UpdatesNeededInRange += CheckForHoposInRange;
        }
    }

    #endregion

    #region Moving

    protected override void InternalMoveSelectionChecks()
    {
        CheckForHoposInRange(mover.GetChangingValidationRange());
    }

    protected override LinkedList<int> GetLaneProgression() => laneOrdering;

    #endregion
    
    #region Add/Delete
    
    protected override void InternalAddDataChecks(int tick, int lane)
    {
        UpdateTickDataToMatch(tick, Lanes.GetLane(lane)[tick]);
        CheckForHopos(tick);
        ClampSustainsBefore(tick, lane);
    }

    #endregion

    #region Selections

    #region Internal Overrides
    
    protected override void InternalSetSelectionToNewLane(int destinationLane)
    {
        var selectionMinMax = Lanes.GetSelectionBounds();
        Lanes.SetSelectionToNewLane(destinationLane);
        CheckForHoposInRange(selectionMinMax.min, selectionMinMax.max);
    }
    
    // FIXME: Only check in the range of the deletion.
    protected override void InternalDeleteChecks()
    {
        CheckForHoposInRange(0, SongTime.SongLengthTicks);
    }
    
    #endregion

    public void NaturalizeSelection()
    {
        var totalSelectionSet = Lanes.GetUnifiedSelection();
        if (totalSelectionSet.Count == 0) return;

        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        
        for (int i = 0; i < Lanes.Count; i++)
        {
            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in totalSelectionSet)
            {
                if (!changingLane.Contains(selectedNote)) continue;

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
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);

        Chart.InPlaceRefresh();
    }

    public void SetSelectionToFlag(FiveFretNoteData.FlagType flag)
    {
        var currentSelection = Lanes.GetUnifiedSelection();
        if (currentSelection.Count == 0) return;
        
        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        
        for (int i = 0; i < Lanes.Count; i++)
        {
            var changingLane = Lanes.GetLane(i);

            foreach (var selectedNote in currentSelection)
            {
                if (!changingLane.Contains(selectedNote)) continue;

                var tickData = changingLane[selectedNote];
                changingLane[selectedNote] = new FiveFretNoteData(tickData.Sustain, flag, false);
            }
        }
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);

        Chart.InPlaceRefresh();
    }

    public void SetEqualSpacing()
    {
        var currentSelection = Lanes.GetTotalSelectionByLane();
        var totalSelectionSet = Lanes.GetUnifiedSelection().ToList();

        // equal spacing has no effect for selections of size 0-2
        if (totalSelectionSet.Count < 3) return;

        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        
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
                var tickData = changingLane.PopSingleTyped(selectedNote);
                if (tickData == default) continue; // default should be FlagType = 0 which is invalid, meaning that data will never exist. Low priority issue

                var index = totalSelectionSet.BinarySearch(selectedNote);
                var equalSpacingTick = (index * evenSpacingDistance) + firstTick;

                changingLane[equalSpacingTick] = tickData;
                laneSelection.Add(equalSpacingTick);
            }
        }
        CheckForHoposInRange(firstTick, lastTick);
        ValidateSustainsInRange(firstTick, lastTick);
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);

        Chart.InPlaceRefresh();
    }

    #endregion

    #region Flag Changes

    private void UpdateTickFlag(int targetTick, FiveFretNoteData.FlagType flag)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            var currentLane = Lanes.GetLane(i);
            if (!currentLane.Contains(targetTick)) continue;
            
            currentLane[targetTick] = currentLane[targetTick].ExportWithNewFlag(flag);
        }
    }

    private void UpdateTickDataToMatch(int tick, FiveFretNoteData data)
    {
        var @default = data.Default;
        var flag = data.Flag;

        for (int i = 0; i < Lanes.Count; i++)
        {
            var activeLane = Lanes.GetLane(i);
            if (!activeLane.Contains(tick)) continue;
            
            var sustain = UserSettings.ExtSustains ? activeLane[tick].Sustain : data.Sustain;
            activeLane[tick] = new FiveFretNoteData(sustain, flag, @default);
        }
    }

    #endregion

    #region HOPOs

    private void CheckForHopos(int changedTick)
    {
        var ticks = Lanes.GetTickEventBounds(changedTick); // biggest bottleneck here btw

        if (IsTickNaturallyChangable(changedTick)) UpdateTickFlag(changedTick, GetTickFlag(changedTick, ticks.prev));
        if (IsTickNaturallyChangable(ticks.next)) UpdateTickFlag(ticks.next, GetTickFlag(ticks.next, changedTick));
    }

    private void CheckForHoposInRange(MinMaxTicks range) => CheckForHoposInRange(range.min, range.max);
    private void CheckForHoposInRange(int startTick, int endTick)
    {
        var uniqueTicks = GetUniqueTickSet();

        int startIndex = uniqueTicks.BinarySearch(startTick);

        if (startIndex < 0)
        {
            startIndex = ~startIndex - 1;
            if (startIndex < 0) startIndex = 0;
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

            if (!IsTickNaturallyChangable(currentTick)) continue;
            
            var flag = IsTickHopo(currentTick, uniqueTicks) ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

            UpdateTickFlag(currentTick, flag);
        }
    }

    // Cannot use IsTickHopo(). IsTickHopo() returns if a tick is CURRENTLY a hopo, not if it WILL be a hopo after a placement.
    public bool PreviewTickHopo(LaneOrientation lane, int tick)
    {
        var ticks = Lanes.GetTickEventBounds(tick);
        
        // Second clause states that placing a note where one already exists when the note count == 1 does not change
        // chord status. Not having this clause causes some issues when Previewers try to repeat add new data to an instrument
        // (if a user holds down for any period of time following an addition of data)
        var isTickChordAfterChange = Lanes.GetTickCountAtTick(tick) > 0 &&
                                        !(Lanes.GetTickCountAtTick(tick) == 1 && Lanes.GetLane((int)lane).Contains(tick));

        return ticks.prev != Lanes<FiveFretNoteData>.NO_TICK_EVENT &&
               tick - ticks.prev < Chart.HopoCutoff &&
               (!isTickChordAfterChange && !Lanes.GetLane((int)lane).Contains(ticks.prev));
    }
    
    private FiveFretNoteData.FlagType GetTickFlag(int tick, int prevTick) =>
        IsTickHopo(tick, prevTick) ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

    /// <remarks>If you've already accessed the UniqueTickSet, don't use this. Will calculate twice.</remarks>
    private bool IsTickHopo(int tick) => IsTickHopo(tick, GetUniqueTickSet());
    private bool IsTickHopo(int tick, List<int> ticks)
    {
        var currentTickIndex = ticks.BinarySearch(tick);
        if (currentTickIndex < 0 || currentTickIndex >= ticks.Count) return false;
        
        var prevTick = currentTickIndex == 0 ? LaneSet<FiveFretNoteData>.NO_TICK_EVENT : ticks[currentTickIndex - 1];
        
        return IsTickHopo(tick, prevTick);
    }
    
    private bool IsTickHopo(int tick, int prevTick)
    {
        return
            // Automatically a strum if this is the first tick in a lane (must be first check)
            (prevTick != LaneSet<FiveFretNoteData>.NO_TICK_EVENT) && 
            // Distance between previous tick must be below the hopo cutoff (second most common reason for no hopo - not intensive op)
            (tick - prevTick < Chart.HopoCutoff) && 
            (
                // If this tick is a chord, disqualified
                Lanes.IsTickChord(tick, out var lastFoundLane) || 
                // If this tick is not a chord, check the last tick again.
                // If the last tick is not a chord and its only tick is in the same lane, disqualified
                // (two single notes in the same lane in a row means the second is a strum)
                (!Lanes.IsTickChord(prevTick) && lastFoundLane.Contains(prevTick))
            );
    }
    
    #endregion

    #region Taps

    public bool IsTickCurrentlyFlag(int tick, FiveFretNoteData.FlagType flag)
    {
        foreach (var lane in Lanes)
        {
            if (!lane.LaneData.TryGetValue(tick, out var data)) continue;
            
            var typed = (FiveFretNoteData)data;
            return typed.Flag == flag;
        }

        throw new ArgumentException("IsTickCurrentlyFlag: Cannot determine current flag status. Tick is not in any lane.");
    }

    private bool IsTickNaturallyChangable(int tick)
    {
        for (int i = 0; i < Lanes.Count; i++)
        {
            var lane = Lanes.GetLane(i);
            if (!lane.Contains(tick)) continue;

            if (lane[tick].Flag == FiveFretNoteData.FlagType.tap || !lane[tick].Default)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Import

    // Prepare for indentation hell.
    protected override void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines)
    {
        // FIXME: This value is already generated in the base class for the undo action.
        HashSet<int> uniqueTicks = lines.Select(item => item.Key).ToHashSet();
        HashSet<int> flippedTicks = new(); // ticks that will be traditionally forced

        SoloEventData openSoloEvent = new(-1);
        
        foreach (var uniqueTick in uniqueTicks)
        {
            var eventsAtTick = lines.Where(item => item.Key == uniqueTick).Select(item => item.Value).ToList();

            // we accept both data ripped straight from a .chart file
            // or special penguin modifiers (resulting from copy/paste action)
            // penguinHopo and penguinStrum correspond to FH (forced hopo) and FS (forced strum) events
            // this is the only place where this is used - this does not happen on export as Penguin uses its own file format
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
                if (identifier.Contains(FORCED_SUBSTRING))
                {
                    forcedModifier = true;
                    eventsAtTick.Remove(identifier);
                }

                if (identifier.Contains(TAP_SUBSTRING))
                {
                    tapModifier = true;
                    eventsAtTick.Remove(identifier);
                }

                if (identifier.Contains(EXPLICIT_HOPO_ID))
                {
                    penguinHopo = true;
                    eventsAtTick.Remove(identifier);
                }

                if (identifier.Contains(EXPLICIT_STRUM_ID))
                {
                    penguinStrum = true;
                    eventsAtTick.Remove(identifier);
                }
            }

            int noteIdentifier;
            int sustain;
            foreach (var @event in eventsAtTick)
            {
                var values = @event.Split(' ');
                switch (values[IDENTIFIER_INDEX])
                {
                    // note (pun not intended):
                    // starpower is parsed seperately via StarpowerInstrument by the Instrument = Tab model
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

                        LaneOrientation lane = MatchChartIDToLaneOrientation(noteIdentifier);

                        bool defaultOrientation = true; // somewhat equivilent to forced

                        // use separate method (CheckForHoposInRange) at end to properly calculate hopo vs. strum
                        // in the meantime, strum is good default
                        FiveFretNoteData.FlagType flagType = FiveFretNoteData.FlagType.strum;
                        if (tapModifier)
                        {
                            flagType = FiveFretNoteData.FlagType.tap; // tap overrides any hopo/forcing logic
                            defaultOrientation = false;
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
                                flippedTicks.Add(uniqueTick);
                            }
                        }

                        // default to strum, will be recalculated later
                        var noteData = new FiveFretNoteData(sustain, flagType, defaultOrientation);

                        Lanes.GetLane((int)lane)[uniqueTick] = noteData;

                        break;
                    case EVENT_INDICATOR:

                        if (!Enum.TryParse(typeof(LocalEventIdentifier), values[EVENT_DATA_INDEX], true, out var localEvent))
                        {
                            Chart.Log($"Error at {uniqueTick}: Unsupported event type: {values[EVENT_DATA_INDEX]}");
                            break;
                        }

                        switch (localEvent)
                        {
                            case LocalEventIdentifier.solo:
                                openSoloEvent = new SoloEventData(uniqueTick);
                                break;
                            case LocalEventIdentifier.soloend:
                                // Techinically a possibility for a soloend event to come before a solo event, even if unlikely.
                                // I will not be having weirdness in MY solo events no thank you
                                if (openSoloEvent.StartTick == -1) continue;

                                openSoloEvent = new SoloEventData(openSoloEvent.StartTick, uniqueTick);
                                SoloData.SoloEvents.Add(openSoloEvent.StartTick, openSoloEvent);

                                openSoloEvent = new SoloEventData(-1);
                                break;
                        }
                        
                        break;
                    case DEPRECATED_HAND_INDICATOR:
                        continue;
                }
            }
        }

        if (openSoloEvent.StartTick >= 0) SoloData.SoloEvents.Add(openSoloEvent.StartTick, openSoloEvent);
        CheckForHoposInRange(uniqueTicks.Min(), uniqueTicks.Max());

        if (Chart.SyncTrackInstrument is not null) ValidateSustainsInRange(uniqueTicks.Min(), uniqueTicks.Max());

        FlipTicks(flippedTicks);
    }

    public void FlipTicks(HashSet<int> flippedTicks)
    {
        foreach (var tick in flippedTicks)
        {
            for (int i = 0; i < Lanes.Count; i++)
            {
                var activeLane = Lanes.GetLane(i);
                if (!activeLane.Contains(tick)) continue;

                var data = activeLane[tick];
                var replaceFlag = data.Flag == FiveFretNoteData.FlagType.strum ? FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;

                activeLane[tick] = new FiveFretNoteData(data.Sustain, replaceFlag, false);
            }
        }
    }

    #endregion

    #region Export
    
    protected override List<string> ConvertEventsToChartStrings()
    {
        var initialEvents = base.ConvertEventsToChartStrings();
        var uniqueTicks = GetUniqueTickSet();
        
        foreach (var tick in GetUniqueTickSet())
        {
            if (IsTickCurrentlyFlag(tick, FiveFretNoteData.FlagType.tap))
            {
                initialEvents.Add($"\t{tick} = {TAP_ID}");
                continue;
            }
            if (IsTickHopo(tick, uniqueTicks) != IsTickCurrentlyFlag(tick, FiveFretNoteData.FlagType.hopo))
            {
                initialEvents.Add($"\t{tick} = {FORCED_SUBSTRING}");
            }
        }

        return initialEvents;
    }

    // no solos currently
    public override string ConvertSelectionToString()
    {
        if (Lanes.GetUnifiedSelection().Count == 0) return "";
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
                    new KeyValuePair<int, string>(note.Key, note.Value.ToChartFormat(i)[0])
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

        stringIDs.AddRange(tapTicks.Select(tick => new KeyValuePair<int, string>(tick, TAP_ID)));
        stringIDs.AddRange(strumTicks.Select(tick => new KeyValuePair<int, string>(tick, EXPLICIT_STRUM_ID)));
        stringIDs.AddRange(hopoTicks.Select(tick => new KeyValuePair<int, string>(tick, EXPLICIT_HOPO_ID)));

        stringIDs.Sort((a, b) => a.Key.CompareTo(b.Key));

        StringBuilder combinedIDs = new();
        foreach (var id in stringIDs)
        {
            combinedIDs.AppendLine($"\t{id.Key} = {id.Value}");
        }
        return combinedIDs.ToString();
    }

    #endregion
}