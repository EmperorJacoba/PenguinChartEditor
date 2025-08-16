using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public interface IEvent<T> : IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler where T : IEventData
{
    /// <summary>
    /// The tick-time timestamp that this event occurs at.
    /// </summary>
    public int Tick { get; set; }

    /// <summary>
    /// Is this event selected?
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// The GameObject with the green highlight that displays selection status to the user.
    /// </summary>
    public GameObject SelectionOverlay { get; set; }

    /// <summary>
    /// Is this event currently visible?
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Is the right-click button currently being held down over this event? (for rclick + lclick functionality)
    /// </summary>
    public bool DeletePrimed { get; set; }

    void CopySelection();
    void PasteSelection();
    void DeleteSelection();

    EventData<T> GetEventData();
    MoveData<T> GetMoveData();
    void SetEvents(SortedDictionary<int, T> newEvents);

}

public abstract class Event<T> : MonoBehaviour, IEvent<T> where T : IEventData
{
    protected InputMap inputMap;
    public int Tick { get; set; }
    public abstract EventData<T> GetEventData();
    public abstract MoveData<T> GetMoveData();
    public abstract void SetEvents(SortedDictionary<int, T> newEvents);
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }
    public bool DeletePrimed { get; set; } // future: make global across events 

    protected virtual void Awake()
    {
        if (!GetEventData().selectionActionsEnabled)
        {
            inputMap = new();
            inputMap.Enable();

            inputMap.Charting.Delete.performed += x => DeleteSelection();
            inputMap.Charting.Copy.performed += x => CopySelection();
            inputMap.Charting.Paste.performed += x => PasteSelection();
            inputMap.Charting.Cut.performed += x => CutSelection();
            inputMap.Charting.Drag.performed += x => MoveSelection();
            inputMap.Charting.LMB.canceled += x => CompleteMove();
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

    public virtual void MoveSelection()
    {
        if (Input.GetKey(KeyCode.LeftControl)) return; // Let BPM labels do their thing undisturbed if applicable

        var moveData = GetMoveData();
        if (BeatlinePreviewer.instance.IsOverlayRaycasterHit() && !moveData.moveInProgress) return;

        var currentMouseTick = SongTimelineManager.CalculateGridSnappedTick(Input.mousePosition.y / Screen.height);

        // early return if no changes to mouse's grid snap
        if (currentMouseTick == moveData.lastMouseTick)
        {
            moveData.lastMouseTick = currentMouseTick;
            return;
        }

        if (!moveData.moveInProgress) // first loop only (init properties)
        {
            moveData.firstMouseTick = currentMouseTick;

            moveData.MovingGhostSet.Clear();

            // first tick of move set should be 0 for correct pasting (localization)
            // so get lowest tick to shift ticks down
            int lowestTick = 0;
            if (GetEventData().Selection.Count > 0)
                lowestTick = GetEventData().Selection.Keys.Min();

            moveData.selectionOriginTick = lowestTick;

            foreach (var selectedTick in GetEventData().Selection)
            {
                moveData.MovingGhostSet.Add(selectedTick.Key - lowestTick, selectedTick.Value);
            }
            if (moveData.MovingGhostSet.Count == 0) return;

            // happens after the moving set init in case no set is created (count = 0)
            moveData.moveInProgress = true;

            moveData.currentMoveAction = new(GetEventData().Events, moveData.MovingGhostSet, lowestTick);


            moveData.lastMouseTick = currentMouseTick;
            moveData.lastTempGhostPasteStartTick = moveData.selectionOriginTick;
            moveData.lastTempGhostPasteEndTick = moveData.selectionOriginTick + moveData.MovingGhostSet.Keys.Max();

            BeatlinePreviewer.instance.gameObject.SetActive(false);

            return;
        }

        if (moveData.MovingGhostSet.Count == 0) return;
        if (GetEventData().Selection.Count > 0) GetEventData().Selection.Clear();

        // Write everything to a temporary dictionary because otherwise tick 0 will not have
        // a correct timestamp of 0 when writing to the main dictionary for BPM events
        // SetEvents() in BPM cleans up data before actually applying the changes, which is required for BPM
        // SetEvents() is already guaranteed by the interface so all event types will have it 
        SortedDictionary<int, T> movingData = new(GetEventData().Events);

        // delete last move preview's data
        var deleteAction = new Delete<T>(movingData);
        deleteAction.Execute(moveData.lastTempGhostPasteStartTick, moveData.lastTempGhostPasteEndTick);

        // re-add any data that was overwritten by last preview
        var keysToReAdd = moveData.currentMoveAction.SaveData.Keys.Where(x => x >= moveData.lastTempGhostPasteStartTick && x <= moveData.lastTempGhostPasteEndTick).ToHashSet();
        foreach (var overwrittenTick in keysToReAdd)
        {
            if (movingData.ContainsKey(overwrittenTick))
            {
                movingData.Remove(overwrittenTick);
            }
            Debug.Log($"Adding (overwrite): {overwrittenTick}");
            movingData.Add(overwrittenTick, moveData.currentMoveAction.SaveData[overwrittenTick]);
        }

        // create new move preview
        var cursorMoveDifference = currentMouseTick - moveData.firstMouseTick;
        var pasteDestination = moveData.selectionOriginTick + cursorMoveDifference;


        foreach (var movingTick in moveData.MovingGhostSet)
        {
            var adjustedTick = movingTick.Key + moveData.selectionOriginTick + cursorMoveDifference;
            if (movingData.ContainsKey(adjustedTick))
            {
                movingData.Remove(adjustedTick);
            }
            movingData.Add(adjustedTick, movingTick.Value);

            // Debug.Log($"Adding (ghostset): {adjustedTick}, resulting from {movingTick.Key} plus {moveData.selectionOriginTick} plus {cursorMoveDifference}.");
        }

        SetEvents(movingData);

        // set up variables for next loop
        moveData.lastMouseTick = currentMouseTick;
        moveData.lastTempGhostPasteStartTick = pasteDestination;
        moveData.lastTempGhostPasteEndTick = moveData.MovingGhostSet.Keys.Max() + pasteDestination;

        // optimize this code
        // fix tick 0 move errors
        // make delete primed universal
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

    public virtual void OnBeginDrag(PointerEventData pointerEventData) { }

    public virtual void OnEndDrag(PointerEventData pointerEventData) {}

    public virtual void OnDrag(PointerEventData pointerEventData) {}

    public virtual void OnPointerClick(PointerEventData pointerEventData)
    {
        
    }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        if (DeletePrimed && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            var deleteAction = new Delete<T>(GetEventData().Events);
            deleteAction.Execute(Tick);
        }

        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = true;
        }

        TempoManager.UpdateBeatlines();

        // Additionally: Temporary move overwrites are never undone 
        // Additionally: Move actions are never committed to an action and thus cannot be undone

    }

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        if (BeatlinePreviewer.justCreated) return;

        if (!DeletePrimed || pointerEventData.button != PointerEventData.InputButton.Left)
        {
            CalculateSelectionStatus(pointerEventData);
        }

        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = false;
        }

        TempoManager.UpdateBeatlines();
    }

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
            selection.Add(Tick, targetEventSet[Tick]);
        }

        // Record the last selection data for shift-click selection
        if (selection.Keys.Contains(Tick)) lastTickSelection = Tick;
    }

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


}
