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

    public void SetSelectionToNewLane(int destinationLane);

    List<int> GetUniqueTickSet();
    
    /// <remarks>
    /// Call from the main thread, because this uses an InputActionMap. Do not call in a constructor!
    /// </remarks>
    void SetUpInputMap();

    string ConvertSelectionToString();
    void AddChartFormattedEventsToInstrument(string clipboardData, int offset);

    void DeleteTicksInSelection();
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
}

public interface ISustainableInstrument
{
    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event);
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
    #region Abstract Implemented Data

    // Override and set to null if the instrument does not have solos.
    public virtual SoloDataSet SoloData { get; set; } = new();
    public InstrumentType InstrumentName { get; set; }
    public DifficultyType Difficulty { get; set; }
    public HeaderType InstrumentID => (HeaderType)((int)InstrumentName + (int)Difficulty);
    public abstract int NoteSelectionCount { get; }
    
    #endregion

    #region Abstract Implemented Funcs

    public abstract bool NoteSelectionContains(int tick, int lane);
    public abstract List<int> GetUniqueTickSet();
    public abstract string ConvertSelectionToString();
    public abstract void AddChartFormattedEventsToInstrument(string clipboardData, int offset);
    public abstract List<string> ExportAllEvents();
    
    public abstract ILaneData GetLaneData(int lane);
    public abstract ILaneData GetBarLaneData();
    public abstract ISelection GetLaneSelection(int lane);
    public abstract bool IsNoteSelectionEmpty();

    #endregion
    
    #region Selections
    
    protected abstract void InternalClearAllSelections();
    public void ClearAllSelections()
    {
        InternalClearAllSelections();
        SoloData?.ClearSelection();
        
        Chart.InPlaceRefresh();
    }

    protected abstract void InternalSelectAll();

    private void SelectAll()
    {
        if (Chart.LoadedInstrument != this) return;
        
        InternalSelectAll();
        SoloData?.SelectAll();
        
        Chart.InPlaceRefresh();
    }

    protected abstract void InternalDeleteSelection();
    private void DeleteSelection()
    {
        // Very important, otherwise if some selections remain in error upon an instrument switch, then data will be
        // unexpectantly deleted.
        if (Chart.LoadedInstrument != this) return;

        SoloData?.DeleteSelection();

        if (NoteSelectionCount != 0)
        {
            InternalDeleteSelection();
        }

        Chart.InPlaceRefresh();
    }

    protected abstract void InternalShiftClickSelectLane(int start, int end, int lane);
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
            InternalShiftClickSelectLane(start, end, lane);
        }
    }

    protected abstract void InternalShiftClickSelect(int start, int end);
    public void ShiftClickSelect(int start, int end)
    {
        if (Chart.LoadedInstrument != this) return;
        ClearAllSelections();
        
        InternalShiftClickSelect(start, end);
        SoloData?.SelectTicksInRange(start, end);
    }

    public void ShiftClickSelect(int tick) => ShiftClickSelect(tick, tick);

    protected abstract void InternalClearTickFromAllSelections(int tick);
    public void ClearTickFromAllSelections(int tick)
    {
        if (Chart.LoadedInstrument != this) return;
        
        InternalClearTickFromAllSelections(tick);
        SoloData.RemoveTickFromAllSelections(tick);
        
        Chart.InPlaceRefresh();
    }

    protected abstract void InternalDeleteTicksInSelection();
    public void DeleteTicksInSelection()
    {
        if (Chart.LoadedInstrument != this) return;
        
        InternalDeleteTicksInSelection();
        SoloData?.DeleteSelection();
        
        Chart.InPlaceRefresh();
    }

    private void CheckForSelectionClear()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || Chart.instance.SceneDetails.IsEventDataHit()) return;

        ClearAllSelections();
    }

    protected abstract void InternalSetSelectionToNewLane(int destinationLane);
    public void SetSelectionToNewLane(int destinationLane)
    {
        if (Chart.LoadedInstrument != this) return;
        
        if (IsNoteSelectionEmpty()) return;
        
        InternalSetSelectionToNewLane(destinationLane);
        
        Chart.InPlaceRefresh();
    }

    #endregion

    #region Delete

    protected abstract void InternalDeleteTickInLane(int tick, int lane);
    public void DeleteTickInLane(int tick, int lane)
    {
        if (Chart.LoadedInstrument != this) return;
        
        if (lane == IInstrument.SOLO_DATA_LANE_ID)
        {
            SoloData?.DeleteTick(tick);
        }
        else
        {
            InternalDeleteTickInLane(tick, lane);
        }
        
        Chart.InPlaceRefresh();
    }

    protected abstract void InternalDeleteAllEventsAtTick(int tick);
    public void DeleteAllEventsAtTick(int tick)
    {
        if (Chart.LoadedInstrument != this) return;
        
        SoloData?.DeleteTick(tick);
        InternalDeleteAllEventsAtTick(tick);
        
        ClearAllSelections();
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

    protected abstract bool InternalMoveSelection();

    private void MoveSelection()
    {
        if (Chart.LoadedInstrument != this || !Chart.IsModificationAllowed()) return;
        
        if (InternalMoveSelection())
        {
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
    
    public void ChangeSustainFromTrail(PointerEventData pointerEventData, IEvent @event) => sustainer.ChangeSustainFromTrail(pointerEventData, @event);
    public void SetSelectionSustain(int ticks) => sustainer.SetSelectionSustain(ticks);
    public void SetSelectionSustain(float bars) => sustainer.SetSelectionSustain(bars);
    public void ValidateSustainsInRange(MinMaxTicks range) => ValidateSustainsInRange(range.min, range.max);
    public void ValidateSustainsInRange(int startTick, int endTick) => sustainer.ValidateSustainsInRange(startTick, endTick);
    public void ClampSustainsBefore(int tick, int lane) => sustainer.ClampSustainsBefore(tick, lane);
    public int CalculateSustainClamp(int sustainLength, int tick, int lane) => sustainer.CalculateSustainClamp(sustainLength, tick, lane);
}

