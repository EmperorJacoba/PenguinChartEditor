using System.Collections.Generic;
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
    List<string> ExportAllEvents();

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

    void PushUndoData(IUndoSnapshot undoSnapshot);
    void SaveUndoData();
}

public interface ISustainableInstrument : IInstrument
{
    public bool ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event);
    public int CalculateSustainClamp(int sustainLength, int tick, int lane);
    public void SetSelectionSustain(int ticks);
    public void SetSelectionSustain(float bars);
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
// after the meat and bones data change happens, which is usally where InternalXXXX() is.

// When you're making your own instrument, I would recommend looking at the most similar instrument to the new instrument
// for a template on what to do. Feel free to ask me for any help!

// - Emperor

// FIXME: With the exception of SyncTrack (my design choice for that one was not the best, but I think it makes more sense
// conceptually), single lane tracks can likely be replaced with a Lanes<T> object with one lane. Investigate this possibility.
// Note: I think it is more clear with one LaneSet<> how the instrument functions, but it is probably easier to implement
// new instruments with a Lanes<> object with only one lane.

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
    public int NoteSelectionCount => LaneController.GetTotalSelectionCount();
    public bool NoteSelectionContains(int tick, int lane) => LaneController.GetLaneSelection(lane).Contains(tick);
    public List<int> GetUniqueTickSet() => LaneController.GetUniqueTickSet();
    public ISelection GetLaneSelection(int lane) => LaneController.GetLaneSelection(lane);
    public bool IsNoteSelectionEmpty() => LaneController.IsSelectionEmpty();
    
    public abstract ILaneData GetBarLaneData();
    
    // Override and set to null if the instrument does not have solos.
    public virtual SoloDataSet SoloData { get; set; } = new();
    
    #endregion

    #region Metadata
    
    public InstrumentType InstrumentName { get; set; }
    public DifficultyType Difficulty { get; set; }
    public HeaderType InstrumentID => (HeaderType)((int)InstrumentName + (int)Difficulty);
    
    #endregion

    #region Undo/Redo

    // FIXME: This functionality can likely be simplified with a unified generic push and save action. Would require an
    // interface that allows exporting and importing data as a Dictionary regardless of lane count. Very doable!
    
    protected abstract void InternalSaveUndoData(UndoSnapshot<T> undoAction);

    public void SaveUndoData()
    {
        ApplyUndoDataToStack(CreateUndoSnapshot());
    }
    
    /// <remarks>
    /// Override ONLY IN SYNCTRACK for the multi-type approach. In all other cases, apply the data to the undoAction
    /// through InternalSaveUndoData().
    /// </remarks>
    protected virtual IUndoSnapshot CreateUndoSnapshot()
    {
        var undoAction = new UndoSnapshot<T>(this);
        InternalSaveUndoData(undoAction);
        return undoAction;
    }

    protected void ApplyUndoDataToStack(IUndoSnapshot undoSnapshot)
    {
        UndoStack.instance.PushAction(undoSnapshot);
    }

    protected abstract void InternalApplyUndoAction(UndoSnapshot<T> undoAction);

    /// <remarks>
    /// Override ONLY IN SYNCTRACK for the multi-type approach. In all other cases, run checks and other needed actions
    /// through InternalApplyUndoAction().
    /// </remarks>
    public virtual void PushUndoData(IUndoSnapshot undoSnapshot)
    {
        var undoAction = undoSnapshot as UndoSnapshot<T>;
        InternalApplyUndoAction(undoAction);
        Chart.InPlaceRefresh();
    }
    
    #endregion

    #region Pasting

    public abstract string ConvertSelectionToString();
    public void PasteDataToInstrument(string clipboardData, int offset)
    {
        SaveUndoData();
        AddChartFormattedEventsToInstrument(clipboardData, offset);
        Chart.InPlaceRefresh();
    }
    protected abstract void AddChartFormattedEventsToInstrument(string clipboardData, int offset);

    #endregion
    
    #region Selections
    
    protected abstract void InternalSetSelectionToNewLane(int destinationLane);
    public void SetSelectionToNewLane(int destinationLane)
    {
        if (Chart.LoadedInstrument != this) return;
     
        SaveUndoData();
        
        if (IsNoteSelectionEmpty()) return;
        
        InternalSetSelectionToNewLane(destinationLane);
        
        Chart.InPlaceRefresh();
    }
    
    public void ClearAllSelections()
    {
        LaneController.ClearAllSelections();
        SoloData?.ClearSelection();
        
        Chart.InPlaceRefresh();
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
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || Chart.instance.SceneDetails.IsEventDataHit()) return;

        ClearAllSelections();
    }
    
    #endregion
    
    #region Add
    
    /// <remarks>
    /// If you need to validate data upon data add, do it here.
    /// </remarks>
    /// <param name="tick"></param>
    /// <param name="lane"></param>
    protected abstract void InternalAddDataChecks(int tick, int lane);
    public void CreateEvent(int tick, int lane, IEventData data)
    {
        ClearAllSelections();
        var snap = CreateUndoSnapshot();
        if (GetLaneData(lane).Add(tick, data))
        {
            ApplyUndoDataToStack(snap);
        }
        else
        {
            return;
        }
        
        InternalAddDataChecks(tick, lane);

        Chart.InPlaceRefresh();
    }
    
    #endregion

    #region Delete

    public void DeleteTickInLane(int tick, int lane)
    {
        if (Chart.LoadedInstrument != this) return;
        SaveUndoData();
        
        if (lane == IInstrument.SOLO_DATA_LANE_ID)
        {
            SoloData?.DeleteTick(tick);
        }
        else
        {
            LaneController.DeleteTickInLane(tick, lane);
        }
        
        Chart.InPlaceRefresh();
    }

    public void DeleteAllEventsAtTick(int tick)
    {
        if (Chart.LoadedInstrument != this) return;
        
        SaveUndoData();
        
        SoloData?.DeleteTick(tick);
        LaneController.DeleteAllEventsAtTick(tick);
        
        ClearAllSelections();
    }
    
    protected virtual void InternalDeleteSelectionChecks() {}

    public void DeleteSelection()
    {
        // Very important, otherwise if some selections remain in error upon an instrument switch, then data will be
        // unexpectantly deleted.
        if (Chart.LoadedInstrument != this) return;
        
        SaveUndoData();

        SoloData?.DeleteSelection();

        if (NoteSelectionCount != 0)
        {
            LaneController.DeleteSelection();
            InternalDeleteSelectionChecks();
        }

        Chart.InPlaceRefresh();
    }
    
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
    
    protected MoveHelper<T> mover = new();

    protected abstract bool InternalMoveSelection(out bool firstFrame);

    private void MoveSelection()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;
        
        if (InternalMoveSelection(out var firstFrame))
        {
            // FIXME: Possible edge case here: user starts moving, and then mid-move, undos. Old data is applied, move lost.
            // Maybe do this on complete move instead? But that has its own issues...
            if (firstFrame) SaveUndoData();
            Chart.InPlaceRefresh();
        }
    }

    protected abstract void InternalCompleteMove();
    private void CompleteMove()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;
        Chart.showPreviewers = true;

        if (!mover.MoveInProgress) return;

        InternalCompleteMove();
        
        mover.Reset();
    }

    #endregion

    #region Export

    public abstract List<string> ExportAllEvents();

    #endregion
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
    
    // Save managed in sustain trail so that undo action reverts to pre-change, not to the last grid-snapped tick
    public bool ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event) => sustainer.ChangeSustainFromTrail(pointerEventData, @event);

    public void SetSelectionSustain(int ticks)
    {
        SaveUndoData();
        sustainer.SetSelectionSustain(ticks);
    }

    public void SetSelectionSustain(float bars)
    {
        SaveUndoData();
        sustainer.SetSelectionSustain(bars);
    }

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

