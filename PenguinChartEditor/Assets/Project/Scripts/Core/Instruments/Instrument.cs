using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// this is untyped because these methods (by design) apply regardless of type. Specify type in calls to an instrument
// if necessary.
public interface IInstrument
{
    public const int SOLO_DATA_LANE_ID = int.MaxValue;
    SoloDataSet SoloData { get; set; }
    InstrumentType InstrumentName { get; set; }
    DifficultyType Difficulty { get; set; }
    HeaderType InstrumentID { get; }
    List<string> ExportDotChartData();

    void ClearAllSelections();
    bool NoteSelectionContains(int tick, int lane);
    int NoteSelectionCount { get; }
    public void ShiftClickSelectLane(int start, int end, int lane);
    public void ShiftClickSelect(int start, int end);
    public void ShiftClickSelect(int tick);
    public void ClearTickFromAllSelections(int tick);

    public void CreateEvent(int tick, int lane, IEventData data);

    public void SetSelectionToNewLane(int destinationLane);

    List<int> GetUniqueTickSet();

    /// <remarks>
    /// Call from the main thread, because this uses an InputActionMap. Do not call in a constructor!
    /// </remarks>
    void SetUpInputMap();

    string ConvertSelectionToString();
    void PasteDataToInstrument(string clipboardData, int offset);

    void DeleteSelection();
    void DeleteTickInLane(int tick, int lane);
    void DeleteAllEventsAtTick(int tick);

    public ILaneData GetLaneData(int lane);

    /// <remarks>
    /// A "bar lane" refers to a lane that a note receiver prefab must respond to in a non-exclusively
    /// fretted view, even if its lane data is not the bar lane data. See: open notes, kick notes.
    /// </remarks>
    public ILaneData GetBarLaneData();

    public ISelection GetLaneSelection(int lane);
    bool IsNoteSelectionEmpty();

    public void UndoAdd(AddSingleDataPackage actionInfo);
    public void RedoAdd(AddSingleDataPackage actionInfo);

    public void RedoDeleteSingle(DeleteSingleDataPackage actionInfo);
    public void UndoDeleteSingle(DeleteSingleDataPackage actionInfo);

    public void RedoDeleteSelection(ISelectionSnapshot actionInfo);
    public void UndoDeleteSelection(ISelectionSnapshot actionInfo);

    public void ReinstateSelectionChange(ISelectionSnapshot incomingSelectionSnapshot,
        ISelectionSnapshot removingSelectionSnapshot);

    ISelectionSnapshot GetEmptySelectionSnapshot();

    public ISelectionSnapshot SnapTicks(List<int> ticks);

    public IInstrument DuplicateToNewInstrument(HeaderType newInstrumentID);
    
    public GameInstrument ActiveGameInstrument { get; set; }
    public int MatchXCoordinateToLane(float xCoordinate);
}

public interface ISustainableInstrument : IInstrument
{
    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event, bool firstFrame);
    public int CalculateSustainClamp(int sustainLength, int tick, int lane);
    public void SetSelectionSustain(int ticks);
    public void SetSelectionSustain(float bars);
    
    public void CompleteOpenSingleSustainUndoAction(IEvent @event);
    public void RedoSingleSustain(SingleSustainDataPackage actionInfo);
    public void UndoSingleSustain(SingleSustainDataPackage actionInfo);
}

// How do I make my own instrument? A starting point:

// The Instrument class is what all instruments MUST inherit from to cleanly fit into the current data structure of
// Penguin. All instruments inherit from BaseInstrument, and instruments with sustain capability (like FiveFretInstrument
// and StarpowerInstrument) inherit from BaseSustainableInstrument.

// They do this to make refreshing on action miles easier.
// For context, I originally just implemented the interface, 
// and every time I would make a new instrument, I would forget to call Chart.InPlaceRefresh() when data changed
// and wonder why the changes weren't showing up properly. :p

// It also contains base capabilities for things like moving, selections, addition, and deletion, as well as tracking actions
// for undo/redo capabilities. 

// To make your own instrument, have it inherit from BaseInstrument or BaseSustainableInstrument, and then set up the data
// structure you need (bare LaneSet<> and SelectionSet<> or Lanes<> object -
// for single lane and multi-lane functionality, respectively - although you can technically use Lanes<> with one lane)

// Plug in your lane data into the obvious overrides and then set up the custom functions you need.
// The "obvious overrides" is anything that starts with "Internal". Internal functions are run at the chokepoint for where
// all customized functionality runs in the base class. This is where the meat and bones functionality is, as well as any checks/validation
// to go along with it. 
// Why Internal instead of overrides? I hate overrides in this capacity. So clunky imo. Also Chart.InPlaceRefresh happens
// after the meat and bones data change happens, which is usually where InternalXXXX() is.

// When you're making your own instrument, I would recommend looking at the most similar instrument to the new instrument
// for a template on what to do. Feel free to ask me for any help!

// - Emperor

/// <summary>
/// This class serves to automatically refresh charts, set up inputs, and call undo/redo actions
/// to avoid running into the common error of
/// forgetting to refresh the chart after implementing certain actions or forgetting to make something undoable.
/// </summary>
public abstract class BaseInstrument<T> : IInstrument where T : IEventData
{
    #region Data Access

    protected abstract IMultiLaneController LaneController { get; }
    
    public ILaneData GetLaneData(int lane) =>
        lane == IInstrument.SOLO_DATA_LANE_ID ? SoloData.SoloEvents : LaneController.GetLane(lane);
    public abstract ILaneData GetBarLaneData(); // so that note receivers can all punch up upon an open note/kick note
    
    public ISelection GetLaneSelection(int lane) => LaneController.GetLaneSelection(lane);
    public bool IsNoteSelectionEmpty() => LaneController.IsSelectionEmpty();
    
    public int NoteSelectionCount => LaneController.GetTotalSelectionCount();
    public bool NoteSelectionContains(int tick, int lane) => LaneController.GetLaneSelection(lane).Contains(tick);
    
    public List<int> GetUniqueTickSet() => LaneController.GetUniqueTickSet();
    
    // Override and set to null if the instrument does not have solos.
    public virtual SoloDataSet SoloData { get; set; } = new();
    
    #endregion

    #region Metadata
    
    public InstrumentType InstrumentName { get; set; }
    public DifficultyType Difficulty { get; set; }
    public HeaderType InstrumentID => (HeaderType)((int)InstrumentName + (int)Difficulty);
    
    #endregion

    #region Undo/Redo

    // Override only in instruments that have special selection snapshot structures. i.e. SyncTrackInstrument
    public virtual ISelectionSnapshot GetEmptySelectionSnapshot() => 
        new SelectionSnapshot<T>(new Dictionary<int, SortedDictionary<int, T>>());
    
    #endregion

    #region Pasting

    public abstract string ConvertSelectionToString();
    public void PasteDataToInstrument(string clipboardData, int offset)
    {
        var undoAction = AddChartFormattedEventsToInstrument(clipboardData, offset);
        if (undoAction is null) return;
        
        UndoStack.instance.PushAction(undoAction);
        Chart.InPlaceRefresh();
    }

    // Overridden in StarpowerInstrument because SP deals with events on an instrument:data basis due to its structure.
    protected virtual PasteSnapshot AddChartFormattedEventsToInstrument(string clipboardData, int offset)
    {
        var lines = Clipboard.ConvertToLineList(clipboardData, offset);
        
        var uniqueTicks = lines.Select(item => item.Key).ToHashSet();
        if (uniqueTicks.Count == 0) return null;

        var minMaxTicks = new MinMaxTicks(uniqueTicks.Min(), uniqueTicks.Max());
        var prePasteSnapshot = LaneController.PopTicksInRange(minMaxTicks.min, minMaxTicks.max);
        
        AddChartFormattedEventsToInstrument(lines);

        var postPasteSnapshot = LaneController.PeekTicksInRange(minMaxTicks.min, minMaxTicks.max);
        
        return new PasteSnapshot(this, new PasteDataPackage(prePasteSnapshot, postPasteSnapshot));
    }
    
    protected abstract void AddChartFormattedEventsToInstrument(List<KeyValuePair<int, string>> lines);

    #endregion

    #region Selections (undoable)
    
    protected abstract void InternalSetSelectionToNewLane(int destinationLane);
    public void SetSelectionToNewLane(int destinationLane)
    {
        if (Chart.LoadedInstrument != this) return;
        if (IsNoteSelectionEmpty()) return;

        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        
        InternalSetSelectionToNewLane(destinationLane);
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);
        
        Chart.InPlaceRefresh();
    }

    public void ReinstateSelectionChange(ISelectionSnapshot incomingSelectionSnapshot, ISelectionSnapshot removingSelectionSnapshot)
    {
        LaneController.ReinstateSelectionSnapshot(incomingSelectionSnapshot, removingSelectionSnapshot);
        ClearAllSelectionsNoRefresh();
    }
    
    #endregion
    
    #region Selections (Non-undoable)
    
    public void ClearAllSelections()
    {
        LaneController.ClearAllSelections();
        SoloData?.ClearSelection();
        
        Chart.InPlaceRefresh();
    }

    private void ClearAllSelectionsNoRefresh()
    {
        LaneController.ClearAllSelections();
        SoloData?.ClearSelection();
    }
    
    private void SelectAll()
    {
        if (Chart.LoadedInstrument != this) return;
        
        LaneController.SelectAll();
        SoloData?.SelectAll();
        
        Chart.InPlaceRefresh();
    }


    public void ShiftClickSelectLane(int start, int end, int lane)
    {
        if (Chart.LoadedInstrument != this) return;
        ClearAllSelections();
        
        if (lane == IInstrument.SOLO_DATA_LANE_ID)
        {
            SoloData?.SelectTicksInRange(start, end);
        }
        else
        {
            LaneController.GetLaneSelection(lane).ShiftClickSelectInRange(start, end);
        }
        
        Chart.InPlaceRefresh();
    }

    protected virtual List<int> targetLanes => null;
    public void ShiftClickSelect(int start, int end)
    {
        if (Chart.LoadedInstrument != this) return;
        ClearAllSelections();

        // override point for StarpowerInstrument
        if (targetLanes is not null)
        {
            LaneController.ShiftClickSelect(start, end, targetLanes);
        }
        else
        {
            LaneController.ShiftClickSelect(start, end);
        }
        SoloData?.SelectTicksInRange(start, end);
        
        Chart.InPlaceRefresh();
    }

    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);

    public void ClearTickFromAllSelections(int tick)
    {
        if (Chart.LoadedInstrument != this) return;
        
        LaneController.ClearTickFromAllSelections(tick);
        SoloData.RemoveTickFromAllSelections(tick);
        
        Chart.InPlaceRefresh();
    }

    private void CheckForSelectionClear()
    {
        if (Chart.IsSceneOverlayUIHit() || Chart.IsEventDataHit()) return;

        ClearAllSelections();
    }
    
    #endregion
    
    #region Add
    
    /// <remarks>
    /// If you need to validate data upon data add, do it here.
    /// </remarks>
    /// <param name="tick"></param>
    /// <param name="lane"></param>
    protected virtual void InternalAddDataChecks(int tick, int lane) {}
    public void CreateEvent(int tick, int lane, IEventData data)
    {
        if (LaneController.CreateEvent(tick, lane, data, out var actionInfo))
        {
            var undoAction = new AddSingleUndoSnapshot(this, actionInfo);
            UndoStack.instance.PushAction(undoAction);
        }
        else
        {
            return;
        }
        
        InternalAddDataChecks(tick, lane);

        ClearAllSelections();
    }

    public void UndoAdd(AddSingleDataPackage actionInfo)
    {
        if (actionInfo.removedDataExists)
            LaneController.CreateEvent(actionInfo.tick, actionInfo.lane, actionInfo.removedData, out _);
        else
            LaneController.DeleteTickInLane(actionInfo.tick, actionInfo.lane);
    }

    public void RedoAdd(AddSingleDataPackage actionInfo)
    {
        LaneController.CreateEvent(actionInfo.tick, actionInfo.lane, actionInfo.addedData, out _);
    }
    
    #endregion

    #region Delete
    
    protected virtual void InternalDeleteChecks() {}

    #region DeleteSingle
    
    public void DeleteTickInLane(int tick, int lane)
    {
        if (Chart.LoadedInstrument != this || !LaneController.IsTickInLane(tick, lane)) return;
        
        if (lane == IInstrument.SOLO_DATA_LANE_ID)
        {
            SoloData?.DeleteTick(tick);
        }
        else
        {
            var poppedData = LaneController.PopTickFromLane(tick, lane);

            // Happens when user tries to delete tick 0 in SyncTrack, which is not a valid operation.
            if (poppedData is null) return;
            
            var undoAction = new DeleteSingleUndoSnapshot(this, new DeleteSingleDataPackage(tick, lane, poppedData));
            UndoStack.instance.PushAction(undoAction);
        }
        
        InternalDeleteChecks();
        Chart.InPlaceRefresh();
    }

    public void UndoDeleteSingle(DeleteSingleDataPackage actionInfo)
    {
        GetLaneData(actionInfo.lane).CreateEvent(actionInfo.tick, actionInfo.deletedData, out _);
    }

    public void RedoDeleteSingle(DeleteSingleDataPackage actionInfo)
    {
        LaneController.PopTickFromLane(actionInfo.tick, actionInfo.lane);
    }
    
    #endregion

    public void DeleteAllEventsAtTick(int tick)
    {
        if (Chart.LoadedInstrument != this) return;

        var undoAction = new DeleteSelectionSnapshot(this, LaneController.SnapTicks(new List<int>(1) { tick }));
        
        SoloData?.DeleteTick(tick);
        LaneController.DeleteAllEventsAtTick(tick);
        
        InternalDeleteChecks();
        ClearAllSelections();

        UndoStack.instance.PushAction(undoAction);
    }

    #region DeleteSelection
    
    public void DeleteSelection()
    {
        // Very important, otherwise if some selections remain in error upon an instrument switch, then data will be
        // unexpectantly deleted.
        if (Chart.LoadedInstrument != this) return;

        SoloData?.DeleteSelection();

        if (NoteSelectionCount != 0)
        {
            var undoAction = new DeleteSelectionSnapshot(this, LaneController.TakeSelectionSnapshot());
            
            LaneController.DeleteSelection();
            
            UndoStack.instance.PushAction(undoAction);
        }

        InternalDeleteChecks();
        Chart.InPlaceRefresh();
    }
    
    public void UndoDeleteSelection(ISelectionSnapshot actionInfo)
    {
        LaneController.ReinstateSelectionSnapshot(actionInfo);
    }

    public void RedoDeleteSelection(ISelectionSnapshot actionInfo)
    {
        LaneController.DeleteFromSelectionSnapshot(actionInfo);
    }
    
    #endregion
    
    #endregion

    #region Inputs

    protected InputMap inputMap;
    
    public virtual void SetUpInputMap()
    {
        inputMap = new InputMap();
        inputMap.Enable();
        
        inputMap.Charting.XYDrag.performed += x => MoveSelection();
        inputMap.Charting.LMB.canceled += x => CompleteMove();
        inputMap.Charting.Delete.performed += x => DeleteSelection();
        inputMap.Charting.LMB.performed += x => CheckForSelectionClear();
        inputMap.Charting.SelectAll.performed += x => SelectAll();
        inputMap.Charting.ClearSelection.performed += x => ClearAllSelections();
    }
    
    
    
    #endregion

    #region Moving

    protected MoveHelper<T> mover
    {
        get
        {
            _m ??= new MoveHelper<T>(this);
            return _m;
        }
    }
    private MoveHelper<T> _m;

    /// <remarks>Run any mid-move checks like hopo checking here.</remarks>
    protected virtual void InternalMoveSelectionChecks() {}

    protected virtual LinkedList<int> GetLaneProgression() => null;

    private void MoveSelection()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;

        if (mover.MoveSelection(LaneController, GetLaneProgression()))
        {
            InternalMoveSelectionChecks();
            Chart.InPlaceRefresh();
        }
    }

    public ISelectionSnapshot SnapTicks(List<int> ticks) => LaneController.SnapTicks(ticks);

    protected virtual void InternalCompleteMoveChecks() {}
    private void CompleteMove()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;
        Chart.showPreviewers = true;

        if (!mover.MoveInProgress) return;
        
        InternalCompleteMoveChecks();
        
        var undoAction = mover.Reset();
        UndoStack.instance.PushAction(undoAction);
    }

    #endregion

    #region Export

    public List<string> ExportDotChartData()
    {
        List<string> notes = ConvertEventsToChartStrings();

        if (SoloData is not null)
        {
            notes.AddRange(SoloData.ExportSoloEventsUnsorted());
        }
        
        notes.AddRange(Chart.StarpowerInstrument.ExportInstrumentStarpowerData(InstrumentID));
        
        var orderedStrings = notes.OrderBy(i => int.Parse(i.Split(" = ")[0])).ToList();
        return orderedStrings;
    }

    protected virtual List<string> ConvertEventsToChartStrings()
    {
        List<string> notes = new();
        foreach (var lanePairing in LaneController)
        {
            notes.AddRange(
                lanePairing.LaneData.
                    Select(note => $"\t{note.tick} = {note.data.ToChartFormat(lanePairing.laneID)[0]}")
            );
        }

        return notes;
    }

    public abstract IInstrument DuplicateToNewInstrument(HeaderType newInstrumentID);

    #endregion

    public GameInstrument ActiveGameInstrument { get; set; }
    
    // This exists because StarpowerInstrument is not really an instrument. It's more of a concept. It will never
    // have a GameInstrument, but lane movements it must support. Thus, a virtual method. Bazinga.
    public virtual int MatchXCoordinateToLane(float xCoordinate)
    {
        return ActiveGameInstrument.MatchXCoordinateToLane(xCoordinate);
    }
}

public abstract class BaseSustainableInstrument<T> : BaseInstrument<T>, ISustainableInstrument where T : IEventData, ISustainable
{
    public override void SetUpInputMap()
    {
        base.SetUpInputMap();
        
        inputMap.Charting.SustainDrag.performed += x => sustainer.SustainSelection();
        inputMap.Charting.RMB.canceled += x => sustainer.ResetSustainChange();
    }
    
    // Remember to initialize in constructor.
    protected SustainHelper<T> sustainer;

    #region SingleSustain (sustain from trail)
    
    private SingleSustainSnapshot openUndoAction;
    // Save managed in sustain trail so that undo action reverts to pre-change, not to the last grid-snapped tick
    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event, bool firstFrame)
    {
        if (firstFrame)
        {
            openUndoAction = new SingleSustainSnapshot(this,
                new SingleSustainDataPackage(@event.Tick, LaneController));
        }

        sustainer.ChangeSustainFromTrail(pointerEventData, @event);
    }

    public void CompleteOpenSingleSustainUndoAction(IEvent @event)
    {
        if (openUndoAction is null) return;
        
        MonoBehaviour.print("Completing empty undo action");
        openUndoAction.CloseAction();
        UndoStack.instance.PushAction(openUndoAction);

        openUndoAction = null;
    }

    public void UndoSingleSustain(SingleSustainDataPackage actionInfo)
    {
        LaneController.ReinstateTickSnapshot(actionInfo.tick, actionInfo.oldData);
    }

    public void RedoSingleSustain(SingleSustainDataPackage actionInfo)
    {
        LaneController.ReinstateTickSnapshot(actionInfo.tick, actionInfo.addedData);
    }
    
    #endregion

    #region SetSelectionSustain
    
    public void SetSelectionSustain(int ticks)
    {
        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        sustainer.SetSelectionSustain(ticks);
        
        undoAction.CloseAction();
        UndoStack.instance.PushAction(undoAction);
    }

    public void SetSelectionSustain(float bars)
    {
        var undoAction = new SelectionChangeSnapshot(this, LaneController);
        sustainer.SetSelectionSustain(bars);
        undoAction.CloseAction();
        
        UndoStack.instance.PushAction(undoAction);
    }
    
    #endregion

    protected override void InternalCompleteMoveChecks()
    {
        mover.SaveCutoffSustainData(LaneController);
        ValidateSustainsInRange(mover.GetFinalValidationRange());
    }

    // ------
    
    // These actions are not undoable because they are internal checks that run after other undoable actions.
    
    protected void ValidateSustainsInRange(MinMaxTicks range) => ValidateSustainsInRange(range.min, range.max);

    protected void ValidateSustainsInRange(int startTick, int endTick)
    {
        sustainer.ValidateSustainsInRange(startTick, endTick);
    }

    protected void ClampSustainsBefore(int tick, int lane)
    {
        sustainer.ClampSustainsBefore(tick, lane);
    }
    public int CalculateSustainClamp(int sustainLength, int tick, int lane) => sustainer.CalculateSustainClamp(sustainLength, tick, lane);
}

