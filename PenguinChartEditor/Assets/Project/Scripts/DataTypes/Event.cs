using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

#region Interface

public interface IEvent<T> : IPointerDownHandler, IPointerUpHandler where T : IEventData
{
    int Tick { get; set; }
    bool Visible { get; set; }
    EventData<T> GetEventData();
    MoveData<T> GetMoveData();
    void SetEvents(SortedDictionary<int, T> newEvents);
}

#endregion

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
    // Stops tick 0 being erased and/or having invalid data when changing the EventData.Events. 
    public abstract void SetEvents(SortedDictionary<int, T> newEvents);

    #endregion

    // Oops! All naming confusion!
    #region Event Handlers

    protected InputMap inputMap;
    protected virtual void Awake()
    {
        // kinda implements a partial singleton where input mapped actions
        // will only occur on one event object of each type
        // w/o boolean guard this will run for every event object and result in needless calculations
        if (!GetEventData().selectionActionsEnabled)
        {
            inputMap = new();
            inputMap.Enable();

            inputMap.Charting.Delete.performed += x => DeleteSelection();
            inputMap.Charting.Copy.performed += x => CopySelection();
            inputMap.Charting.Paste.performed += x => PasteSelection();
            inputMap.Charting.Cut.performed += x => CutSelection();
            inputMap.Charting.Drag.performed += x => MoveSelection(); // runs every frame drag is active
            inputMap.Charting.LMB.canceled += x => CompleteMove(); // runs ONLY when move action is completed; this wraps up the move action
            inputMap.Charting.LMB.performed += x => CheckForSelectionClear();
            inputMap.Charting.RMB.performed += x => GetEventData().RMBHeld = true;
            inputMap.Charting.RMB.canceled += x => GetEventData().RMBHeld = false;
            GetEventData().selectionActionsEnabled = true;
        }
    }

    public void CopySelection()
    {
        GetEventData().Clipboard.Clear();
        var copyAction = new Copy<T>(GetEventData().Events);
        copyAction.Execute(GetEventData().Clipboard, GetEventData().Selection);
    }

    public virtual void PasteSelection()
    {
        var pasteAction = new Paste<T>(GetEventData().Events);
        pasteAction.Execute(BeatlinePreviewer.currentPreviewTick, GetEventData().Clipboard);
        TempoManager.UpdateBeatlines();
    }

    public virtual void CutSelection()
    {
        var cutAction = new Cut<T>(GetEventData().Events);
        cutAction.Execute(GetEventData().Clipboard, GetEventData().Selection);
    }

    public virtual void DeleteSelection()
    {
        var deleteAction = new Delete<T>(GetEventData().Events);
        deleteAction.Execute(GetEventData().Selection);
        TempoManager.UpdateBeatlines();
    }

    public virtual void CreateEvent(int newTick, T newData)
    {
        var createAction = new Create<T>(GetEventData().Events);
        createAction.Execute(newTick, newData, GetEventData().Selection);
        TempoManager.UpdateBeatlines();
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
        if (BeatlinePreviewer.instance.IsRaycasterHit(BeatlinePreviewer.instance.overlayUIRaycaster) && !moveData.moveInProgress) return;

        var currentMouseTick = SongTimelineManager.CalculateGridSnappedTick(Input.mousePosition.y / Screen.height);

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
        if (moveData.MovingGhostSet.Count == 0) return;

        // temporarily clear selection to avoid selection jank while moving
        // (items that are not selected could appear selected and vice versa b/c of selection logic)
        // selection gets restored upon drag ending
        if (GetEventData().Selection.Count > 0) GetEventData().Selection.Clear();

        // Write everything to a temporary dictionary because otherwise when moving from t=0
        // tick 0 will not exist in the dictionary for TS & BPM events, which are needed
        // SetEvents() in BPM/TS cleans up data before actually applying the changes, which is required for BPM/TS
        // SetEvents() is already guaranteed by the interface so all event types will have it 
        SortedDictionary<int, T> movingData = new(GetEventData().Events);

        // delete last move preview's data
        var deleteAction = new Delete<T>(movingData);
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
        else return; // nothing to move
        moveData.selectionOriginTick = lowestTick;

        foreach (var selectedTick in GetEventData().Selection)
        {
            moveData.MovingGhostSet.Add(selectedTick.Key - lowestTick, selectedTick.Value);
        }

        moveData.firstMouseTick = currentMouseTick;
        moveData.lastMouseTick = currentMouseTick;

        // happens after the moving set init in case no set is created (count = 0)
        moveData.moveInProgress = true;

        moveData.currentMoveAction = new(GetEventData().Events, moveData.MovingGhostSet, lowestTick);
        moveData.lastTempGhostPasteStartTick = moveData.selectionOriginTick;

        BeatlinePreviewer.instance.gameObject.SetActive(false);
    }

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


        BeatlinePreviewer.instance.gameObject.SetActive(true);

        TempoManager.UpdateBeatlines();
    }

    public virtual void CheckForSelectionClear()
    {
        if (!BeatlinePreviewer.instance.IsRaycasterHit(BeatlinePreviewer.instance.beatlineCanvasRaycaster))
        {
            GetEventData().Selection.Clear();
        }
        TempoManager.UpdateBeatlines();
    }

    public virtual void OnPointerDown(PointerEventData pointerEventData)
    {
        if (GetEventData().RMBHeld && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            var deleteAction = new Delete<T>(GetEventData().Events);
            deleteAction.Execute(Tick);
        }

        TempoManager.UpdateBeatlines();
    }

    public virtual void OnPointerUp(PointerEventData pointerEventData)
    {
        if (BeatlinePreviewer.justCreated) return;

        if (!GetEventData().RMBHeld || pointerEventData.button != PointerEventData.InputButton.Left)
        {
            CalculateSelectionStatus(pointerEventData);
        }

        TempoManager.UpdateBeatlines();
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
        SortedDictionary<int, T> targetEventSet = GetEventData().Events;

        // Goal is to follow standard selection functionality of most productivity programs
        if (clickData.button != PointerEventData.InputButton.Left) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            selection.Clear();

            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            var selectedEvents = targetEventSet.Keys.ToList().Where(x => x <= maxNum && x >= minNum);
            foreach (var tick in selectedEvents)
            {
                selection.Add(tick, targetEventSet[tick]);
            }
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
            selection.Clear();
            if (targetEventSet.ContainsKey(Tick)) selection.Add(Tick, targetEventSet[Tick]);
        }

        // Record the last selection data for shift-click selection
        if (selection.Keys.Contains(Tick)) lastTickSelection = Tick;
    }

    #endregion
}
