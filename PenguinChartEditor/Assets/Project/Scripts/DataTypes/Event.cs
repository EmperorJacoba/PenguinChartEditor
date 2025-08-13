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
        var moveData = GetMoveData();

        if (Input.GetKey(KeyCode.LeftControl)) return; // Let BPM labels do their thing undisturbed if applicable

        var currentMouseTick = SongTimelineManager.CalculateGridSnappedTick(Input.mousePosition.y / Screen.height);
        if (currentMouseTick == moveData.lastMouseTick)
        {
            moveData.lastMouseTick = currentMouseTick;
            return;
        }

        if (!moveData.moveInProgress )
        {
            Debug.Log($"Begin move");

            moveData.firstMouseTick = currentMouseTick;
            moveData.currentMoveAction = new(GetEventData().Events);
            moveData.MovingGhostSet.Clear();

            int lowestTick = 0;
            if (GetEventData().Selection.Count > 0) lowestTick = GetEventData().Selection.Keys.Min();
            moveData.selectionOriginTick = lowestTick;

            // add relevant data for each tick into clipboard
            foreach (var selectedTick in GetEventData().Selection)
            {
                moveData.MovingGhostSet.Add(selectedTick.Key - lowestTick, selectedTick.Value);
            }
            moveData.moveInProgress = true;

            moveData.lastMouseTick = currentMouseTick;
            moveData.lastMoveGhostPaste = moveData.selectionOriginTick;

            BeatlinePreviewer.instance.gameObject.SetActive(false);

            return;
        }

        if (moveData.MovingGhostSet.Count == 0) return;

        if (GetEventData().Selection.Count > 0) GetEventData().Selection.Clear();

        var deleteAction = new Delete<T>(GetEventData().Events);
        deleteAction.Execute(moveData.lastMoveGhostPaste, moveData.MovingGhostSet.Keys.Max() + moveData.lastMoveGhostPaste);

        var pasteAction = new Paste<T>(GetEventData().Events);
        pasteAction.Execute(currentMouseTick, moveData.MovingGhostSet);

        moveData.lastMouseTick = currentMouseTick;
        moveData.lastMoveGhostPaste = currentMouseTick;
        // First frame: save state of modified event dictionary for EditAction <//>
        // Also move selection into event set's moving ghost set <//>

        // Every frame, calculate the tick position of the mouse                    (STLM @ CalculateGridSnappedTick)
        // Old position of the moving ghost set is pulverized, if applicable        (Cut action @ old position)
        // Ghost set is inserted into dictionary at new point                       (Paste action @ new position)
        // Record current position of the pasted moving ghost set                   

        
    }

    public virtual void CompleteMove()
    {
        // On "cancel" (CompleteMove), record edit action and put in undo stack

        if (!GetMoveData().moveInProgress) return;
        Debug.Log($"Finished!");
        GetMoveData().moveInProgress = false;

        GetEventData().Selection.Clear();
        foreach (var item in GetMoveData().MovingGhostSet)
        {
            GetEventData().Selection.Add(item.Key + GetMoveData().lastMoveGhostPaste, item.Value);
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
        if (BeatlinePreviewer.justCreated) return;
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = true;
        }

        if (!DeletePrimed || pointerEventData.button != PointerEventData.InputButton.Left)
        {
            CalculateSelectionStatus(pointerEventData);
        }

        if (DeletePrimed && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            DeleteSelection();
        }

        TempoManager.UpdateBeatlines();

        // Goal: Get move to work when dragging over a label object
        // Currently, move will only work properly when dragging outside of the label
        // When dragging on the label, the label is exclusively selected and then only that label moves.
        // Additionally: Temporary move overwrites are never undone 
        // Additionally: Move actions are never committed to an action and thus cannot be undone

    }

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = false;
        }
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

        // Shift-click functionality
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
        // Left control if item is already selected
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
