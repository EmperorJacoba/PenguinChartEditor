using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

public interface IEvent<T> : IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
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
    void SetEvents(SortedDictionary<int, T> newEvents);

}

public abstract class Event<T> : MonoBehaviour, IEvent<T>
{
    protected InputMap inputMap;
    public int Tick { get; set; }
    public abstract EventData<T> GetEventData();
    public abstract void SetEvents(SortedDictionary<int, T> newEvents);
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }
    public bool DeletePrimed { get; set; } // future: make global across events 

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

        // paste currently crashes when paste zone exceeds the screen - fix
        // implement other event actions!!
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

    public virtual void OnBeginDrag(PointerEventData pointerEventData)
    {
        Debug.Log($"Beging");

       // GetEventData().currentMoveAction = new(GetEventData().Events);
        //GetEventData().currentMoveAction.BeginMove(GetEventData().MovingGhostSet, GetEventData().Selection);
    }

    public virtual void OnEndDrag(PointerEventData pointerEventData)
    {
        Debug.Log($"Ending");
        // GetEventData().currentMoveAction.Execute(0, GetEventData().MovingGhostSet);
    }

    public virtual void OnDrag(PointerEventData pointerEventData)
    {
        // Have ghost of selection showing where the selection will go
    }

    public virtual void OnPointerClick(PointerEventData pointerEventData)
    {
        Debug.Log($"Check running");
        CalculateSelectionStatus(pointerEventData.button);

        if (DeletePrimed && pointerEventData.button == PointerEventData.InputButton.Left) DeleteSelection();

        TempoManager.UpdateBeatlines();
        Debug.Log($"{GetEventData().Selection.Count}");
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
        if (GetEventData().Selection.Contains(Tick)) return true;
        else return false;
    }

    public static int lastTickSelection;
    /// <summary>
    /// Calculate the event(s) to be selected based on the last click event.
    /// </summary>
    /// <param name="clickButton">PointerEventData.button</param>
    /// <param name="targetSelectionSet">The selection hash set that contains this event type's selection data.</param>
    /// <param name="targetEventSet">The keys of a sorted dictionary that holds event data (beatlines, TS, etc)</param>
    public void CalculateSelectionStatus(PointerEventData.InputButton clickButton)
    {
        Debug.Log($"clicked");
        var selection = GetEventData().Selection;
        List<int> targetEventSet = GetEventData().Events.Keys.ToList();

        // Goal is to follow standard selection functionality of most productivity programs
        if (clickButton != PointerEventData.InputButton.Left) return;

        // Shift-click functionality
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log($"lshift");
            selection.Clear();

            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            HashSet<int> selectedEvents = targetEventSet.Where(x => x <= maxNum && x >= minNum).ToHashSet();
            selection.UnionWith(selectedEvents);
        }
        // Left control if item is already selected
        else if (Input.GetKey(KeyCode.LeftControl) && selection.Contains(Tick))
        {
            Debug.Log($"lcontrol");
            selection.Remove(Tick);
            return; // prevent lastTickSelection from being stored as an unselected number
        }
        // Left control if item is not currently selected
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            Debug.Log($"lcontrol no delete");
            selection.Add(Tick);
        }
        // Regular click, no extra significant keybinds
        else
        {
            Debug.Log($"reg");
            selection.Clear();
            selection.Add(Tick);
        }
        Debug.Log($"exit");
        // Record the last selection data for shift-click selection
        lastTickSelection = Tick;
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

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = true;
        }
    }

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = false;
        }
    }
}
