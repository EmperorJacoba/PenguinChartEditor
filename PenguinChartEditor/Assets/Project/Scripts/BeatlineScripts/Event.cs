using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

public interface IEvent<DataType>
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

    void HandlePointerDown(BaseEventData baseEventData);
    void HandlePointerUp(BaseEventData baseEventData);
    void HandleDragEvent(BaseEventData baseEventData);

    SortedDictionary<int, DataType> GetEventClipboard();
    
    HashSet<int> GetSelectedEvents();

    void CopySelection();
    void PasteSelection();
    void DeleteSelection();

    // Get and set functions are required for common abstract functions (ex. copy/paste)
    SortedDictionary<int, DataType> GetEvents();
    void SetEvents(SortedDictionary<int, DataType> newEvents);

}

[RequireComponent(typeof(EventTrigger))]
public abstract class Event<DataType> : MonoBehaviour, IEvent<DataType>
{
    protected InputMap inputMap;
    public int Tick { get; set; }
    public abstract HashSet<int> GetSelectedEvents();
    public abstract SortedDictionary<int, DataType> GetEventClipboard();
    public abstract SortedDictionary<int, DataType> GetEvents();
    public abstract void HandleDragEvent(BaseEventData baseEventData);
    public abstract void SetEvents(SortedDictionary<int, DataType> newEvents);
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }
    public bool DeletePrimed { get; set; } // future: make global across events 

    public void CopySelection()
    {
        GetEventClipboard().Clear();
        var copyAction = new Copy<DataType>(GetEvents());
        copyAction.Execute(GetEventClipboard(), GetSelectedEvents());
    }

    public virtual void PasteSelection()
    {
        var pasteAction = new Paste<DataType>(GetEvents());
        pasteAction.Execute(BeatlinePreviewer.currentPreviewTick, GetEventClipboard());
        TempoManager.UpdateBeatlines();

        // paste currently crashes when paste zone exceeds the screen - fix
        // implement other event actions!!
    }

    public virtual void CutSelection()
    {
        var cutAction = new Cut<DataType>(GetEvents());
        cutAction.Execute(GetEventClipboard(), GetSelectedEvents());
    }

    public virtual void DeleteSelection()
    {
        var deleteAction = new Delete<DataType>(GetEvents());
        deleteAction.Execute(GetSelectedEvents());
        TempoManager.UpdateBeatlines();
    }

    public virtual void CreateEvent(int newTick, DataType newData)
    {
        var createAction = new Create<DataType>(GetEvents());
        createAction.Execute(newTick, newData, GetSelectedEvents());
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
        if (GetSelectedEvents().Contains(Tick)) return true;
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
        var selection = GetSelectedEvents();
        List<int> targetEventSet = GetEvents().Keys.ToList();

        // Goal is to follow standard selection functionality of most productivity programs
        if (clickButton != PointerEventData.InputButton.Left) return;

        // Shift-click functionality
        if (Input.GetKey(KeyCode.LeftShift))
        {
            selection.Clear();

            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            HashSet<int> selectedEvents = targetEventSet.Where(x => x <= maxNum && x >= minNum).ToHashSet();
            selection.UnionWith(selectedEvents);
        }
        // Left control if item is already selected
        else if (Input.GetKey(KeyCode.LeftControl) && selection.Contains(Tick))
        {
            selection.Remove(Tick);
            return; // prevent lastTickSelection from being stored as an unselected number
        }
        // Left control if item is not currently selected
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            selection.Add(Tick);
        }
        // Regular click, no extra significant keybinds
        else
        {
            selection.Clear();
            selection.Add(Tick);
        }
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

    public void HandlePointerDown(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = true;
        }
    }

    public void HandlePointerUp(BaseEventData baseEventData)
    {
        var pointerData = (PointerEventData)baseEventData;
        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            DeletePrimed = false;
        }
    }
}
