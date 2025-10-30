using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

#region Interface

public interface IEvent<T> : IPointerDownHandler, IPointerUpHandler where T : IEventData
{
    int Tick { get; set; }
    bool Visible { get; set; }

    EventData<T> GetEventData();
    MoveData<T> GetMoveData();
    SortedDictionary<int, T> GetEventSet();
    IInstrument parentInstrument { get; }
    IPreviewer EventPreviewer { get; }

    void SetEvents(SortedDictionary<int, T> newEvents);
    void RefreshLane();

    // So that Lane<T> can access these easily
    void DeleteSelection();
    void CopySelection();
    void PasteSelection();
    void CutSelection();
    void MoveSelection();
    void SustainSelection();
    void CompleteSustain();
    void CompleteMove();
    void CheckForSelectionClear();
    void SelectAllEvents();
}

#endregion

// Note about how event handlers are managed versus data contained within each object itself
// Event handlers (like a click + drag from the input map) are handled within Lane<T>
// and then sent to the Lane's event reference (usually the component on its previewer for simplicity; it always exists)
// Then the Event<T> component on the previewer will handle the "event action" (like copy/paste/sustain/etc)
// At the end of the component's handling, it calls a refresh of the chart
// (or lane, depending => some need full refresh, like labels; notes do not need full refresh)
// so important distinction: anything to do with an input happens on ONE EVENT<T> OBJECT, so you cannot reference the Tick property
// possible refactor: split these functions up to avoid the dual purpose of this class

// Each event (including one tasked with event handler) has an assigned lane 
// Lane assignment happens through GetEventSet() and GetEventData(), which is just a reference to its "instrument" lane data.
public abstract class Event<T> : MonoBehaviour, IEvent<T> where T : IEventData
{
    #region Properties
    public int Tick { get; set; }
    public bool Selected
    {
        get
        {
            return _selected;
        }
        set
        {
           // Debug.Log($"Selection property accessed. {Tick}");
            SelectionOverlay.SetActive(value);
            _selected = value;
        }
    }
    bool _selected = false;
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }

    #endregion

    #region Static Set Access Points

    // These functions point to each event type's static data members 
    // so they can be used in the broad "Event" class.
    // For all event and move data, only one exists for each set, but
    // static members cannot be accessed directly without these functions.
    public abstract EventData<T> GetEventData();
    public abstract MoveData<T> GetMoveData();

    // Used to clean up input data before actually committing event changes to dictionaries
    // Stops tick 0 being erased and/or having invalid data when changing EventData.Events. 
    public abstract void SetEvents(SortedDictionary<int, T> newEvents);

    public abstract SortedDictionary<int, T> GetEventSet();
    public abstract void RefreshLane();
    public abstract IPreviewer EventPreviewer { get; }
    public abstract void SustainSelection();
    public abstract void CompleteSustain();
    public abstract IInstrument parentInstrument { get; }
    #endregion

    // Oops! All naming confusion!
    #region Event Handlers

    /// <summary>
    /// Used to prevent the TS and BPM events at tick 0 from being deleted.
    /// If TS and BPM events at tick 0 are deleted, the chart has no place to start its beatline calculations from.
    /// [SyncTrack] must ALWAYS have one BPM and one TS event at tick 0.
    /// Users should edit tick 0 events for TS & BPM, not delete them.
    /// </summary>
    protected virtual bool tick0Immune { get; set; } = false;

    public void CopySelection()
    {
        GetEventData().Clipboard.Clear();
        var copyAction = new Copy<T>(GetEventSet());
        copyAction.Execute(GetEventData().Clipboard, GetEventData().Selection);
    }

    public virtual void PasteSelection()
    {
        var pasteAction = new Paste<T>(GetEventSet(), tick0Immune);
        pasteAction.Execute(EventPreviewer.Tick, GetEventData().Clipboard);
        Chart.Refresh();
    }

    public virtual void CutSelection()
    {
        var cutAction = new Cut<T>(GetEventSet(), tick0Immune);
        cutAction.Execute(GetEventData().Clipboard, GetEventData().Selection);
        Chart.Refresh();
    }

    public virtual void DeleteSelection()
    {
        var deleteAction = new Delete<T>(GetEventSet(), tick0Immune);
        deleteAction.Execute(GetEventData().Selection);
        Chart.Refresh();
    }

    public virtual void CreateEvent(int newTick, T newData)
    {
        var createAction = new Create<T>(GetEventSet());
        createAction.Execute(newTick, newData, GetEventData().Selection);
        Chart.Refresh();
    }

    /// <summary>
    /// Runs every frame when Drag input action is active. 
    /// </summary>
    public virtual void MoveSelection()
    {
        if (Input.GetKey(KeyCode.LeftControl)) return; // Let BPM labels do their thing undisturbed if applicable

        var moveData = GetMoveData();

        // Early return if attempting to start a move while over an overlay element
        // Allows moves to start only if interacting with main content
        if (EventPreviewer.IsOverlayUIHit() && !moveData.moveInProgress)
        {
            return;
        }

        float highwayPercent;
        if (Chart.currentTab == Chart.TabType.TempoMap)
        {
            highwayPercent = Input.mousePosition.y / Screen.height;
        }
        else
        {
            // this throws a not implemented exception if this is called on a previewer not on a 3D scene
            highwayPercent = EventPreviewer.GetCursorHighwayProportion();
        }

        var currentMouseTick = SongTime.CalculateGridSnappedTick(highwayPercent);

        // early return if no changes to mouse's grid snap
        if (currentMouseTick == moveData.lastMouseTick)
        {
            moveData.lastMouseTick = currentMouseTick;
            return;
        }

        if (!moveData.moveInProgress)
        {
            InitializeMoveAction(currentMouseTick);
            return;
        }

        // in some cases this runs even when nothing is moving which will throw an error 
        if (moveData.MovingGhostSet.Count == 0)
        {
            return;
        }

        // temporarily clear selection to avoid selection jank while moving
        // (items that are not selected could appear selected and vice versa b/c of selection logic)
        // selection gets restored upon drag ending
        if (GetEventData().Selection.Count > 0)
        {
            GetEventData().Selection.Clear();
        }

        // Write everything to a temporary dictionary because otherwise when moving from t=0
        // tick 0 will not exist in the dictionary for TS & BPM events, which are needed
        // SetEvents() in BPM/TS cleans up data before actually applying the changes, which is required for BPM/TS
        // SetEvents() is already guaranteed by the interface so all event types will have it 
        SortedDictionary<int, T> movingData = new(GetEventSet());

        // delete last move preview's data
        var deleteAction = new Delete<T>(movingData, tick0Immune);
        deleteAction.Execute(moveData.lastTempGhostPasteStartTick, moveData.lastTempGhostPasteEndTick);

        // re-add any data that was overwritten by last preview
        // SaveData holds state of dictionary before move action without the moving objects 
        // (moving objects themselves are stored in poppedData for needed uses)
        var keysToReAdd = moveData.currentMoveAction.SaveData.Keys.Where(x => x >= moveData.lastTempGhostPasteStartTick && x <= moveData.lastTempGhostPasteEndTick).ToHashSet();
        foreach (var overwrittenTick in keysToReAdd)
        {
            if (movingData.ContainsKey(overwrittenTick))
            {
                movingData.Remove(overwrittenTick);
            }
            movingData.Add(overwrittenTick, moveData.currentMoveAction.SaveData[overwrittenTick]);
        }

        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;

        // base the paste on how the cursor moves so that the selection can move
        // in parallel to the mouse (which is how moving should work intuitively)
        var pasteDestination = moveData.selectionOriginTick + cursorMoveDifference;

        foreach (var movingTick in moveData.MovingGhostSet)
        {
            var targetPasteTick = pasteDestination + movingTick.Key;
            if (movingData.ContainsKey(targetPasteTick))
            {
                movingData.Remove(targetPasteTick);
            }
            movingData.Add(targetPasteTick, movingTick.Value);
        }

        SetEvents(movingData);
        Chart.Refresh();

        // set up variables for next loop
        moveData.lastMouseTick = currentMouseTick;
        moveData.lastTempGhostPasteStartTick = pasteDestination;
    }

    void InitializeMoveAction(int currentMouseTick)
    {
        var moveData = GetMoveData();
        moveData.MovingGhostSet.Clear();

        // first tick of move set should be 0 for correct pasting (localization)
        // so get lowest tick to shift ticks down
        int lowestTick;
        if (GetEventData().Selection.Count > 0)
        {
            lowestTick = GetEventData().Selection.Keys.Min();
        }
        else
        {
            return; // nothing to move
        }
        moveData.selectionOriginTick = lowestTick;

        foreach (var selectedTick in GetEventData().Selection)
        {
            moveData.MovingGhostSet.Add(selectedTick.Key - lowestTick, selectedTick.Value);
        }

        moveData.firstMouseTick = currentMouseTick;
        moveData.lastMouseTick = currentMouseTick;

        // happens after the moving set init in case no set is created (count = 0)
        moveData.moveInProgress = true;

        moveData.currentMoveAction = new(GetEventSet(), moveData.MovingGhostSet, lowestTick);
        moveData.lastTempGhostPasteStartTick = moveData.selectionOriginTick;

        EventPreviewer.Hide();
    }

    static bool justMoved = false;
    public virtual void CompleteMove()
    {
        // On "cancel" (CompleteMove), record edit action and put in undo stack

        if (!GetMoveData().moveInProgress) return;

        GetMoveData().moveInProgress = false;

        GetEventData().Selection.Clear();
        foreach (var item in GetMoveData().MovingGhostSet)
        {
            GetEventData().Selection.Add(item.Key + GetMoveData().lastTempGhostPasteStartTick, item.Value);
        }

        EventPreviewer.Show();

        justMoved = true;

        Chart.Refresh();
    }

    public virtual void CheckForSelectionClear()
    {
        if (EventPreviewer.IsOverlayUIHit() || EventPreviewer.AreLaneObjectsHit()) return;

        GetEventData().Selection.Clear();
        RefreshLane();
    }

    public void SelectAllEvents()
    {
        GetEventData().Selection.Clear();
        foreach (var item in GetEventSet())
        {
            GetEventData().Selection.Add(item.Key, item.Value);
        }
        if (GetEventData().Selection.Count != 0) RefreshLane();
    }

    public static bool justDeleted = false;
    public virtual void OnPointerDown(PointerEventData pointerEventData)
    {
        // used for right click + left click delete functionality
        if (GetEventData().RMBHeld && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            var deleteAction = new Delete<T>(GetEventSet(), tick0Immune);
            justDeleted = deleteAction.Execute(Tick);

            GetEventData().Selection.Remove(Tick); // otherwise it will lay dorment and screw up anything to do with selections
            disableNextSelectionCheck = true;

            Chart.Refresh();
        }
    }
    protected bool disableNextSelectionCheck = false; // used in charted instruments only

    public virtual void OnPointerUp(PointerEventData pointerEventData)
    {
        // stops this from firing on the release after clicking to create an event
        if (EventPreviewer.disableNextSelectionCheck)
        {
            EventPreviewer.disableNextSelectionCheck = false;
            return;
        }

        // move action's selection logic is very particular
        // needs to reinstate old selection after move completes, and that happens BEFORE the CalculateSelectionStatus() call,
        // so the reinstated selection immediately gets overwritten as a result
        if (justMoved)
        {
            justMoved = false;
            return;
        }

        if (!GetEventData().RMBHeld || pointerEventData.button != PointerEventData.InputButton.Left)
        {
            CalculateSelectionStatus(pointerEventData);
            RefreshLane();
        }
    }

    #endregion

    #region Selection Logic

    public bool CheckForSelection()
    {
        if (GetEventData().Selection.Keys.Contains(Tick)) return true;
        else return false;
    }

    public static int lastTickSelection;
    /// <summary>
    /// Calculate the event(s) to be selected based on the last click event.
    /// </summary>
    /// <param name="clickButton">PointerEventData.button</param>
    public void CalculateSelectionStatus(PointerEventData clickData)
    {
        var selection = GetEventData().Selection;
        SortedDictionary<int, T> targetEventSet = GetEventSet();

        // Goal is to follow standard selection functionality of most productivity programs
        if (clickData.button != PointerEventData.InputButton.Left) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            parentInstrument.ShiftClickSelect(minNum, maxNum);
            Chart.Refresh();
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            if (selection.Keys.Contains(Tick))
            {
                selection.Remove(Tick);
            }
            else
            {
                selection.Add(Tick, targetEventSet[Tick]);
            }
        }
        // Regular click, no extra significant keybinds
        else
        {
            parentInstrument.ClearAllSelections();
            if (targetEventSet.ContainsKey(Tick)) selection.Add(Tick, targetEventSet[Tick]);
        }

        // Record the last selection data for shift-click selection
        if (selection.Keys.Contains(Tick)) lastTickSelection = Tick;
    }

    public void AddToSelection()
    {
        if (GetEventData().Selection.ContainsKey(Tick)) return;
        GetEventData().Selection.Add(Tick, GetEventSet()[Tick]);
    }

    public void RemoveFromSelection()
    {
        if (!GetEventData().Selection.ContainsKey(Tick)) return;
        GetEventData().Selection.Remove(Tick);
    }

    #endregion
}
