using System;
using System.Collections;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.EventSystems;

#region Interface

public interface IEvent
{
    int Tick { get; }
    bool Visible { get; set; }

    // Used in previewer to check placement conditions
    ISelection GetSelection();
    ILaneData GetLaneData();

    IInstrument ParentInstrument { get; }
    IPreviewer EventPreviewer { get; }

    void RefreshLane();
    bool IsPreviewEvent { get; set; }
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
public abstract class Event<T> : MonoBehaviour, IEvent, IPointerDownHandler where T : IEventData
{
    public abstract int Lane { get; }
    protected static float doubleClickTime = 0.3f;
    #region Properties
    public int Tick
    {
        get
        {
            return _tick;
        }
    }
    protected int _tick;

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

    private const int RMB_ID = 1;
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

    #region Data Access

    // These properties point to each event type's instrument data
    // so they can be used in the broad "Event" class.
    // Data is always stored in an instrument, or in the BPM/TS case, in the Tempo/TimeSignature classes.
    public abstract SelectionSet<T> Selection { get; }
    public ISelection GetSelection() => Selection;

    public abstract LaneSet<T> LaneData { get; }
    public ILaneData GetLaneData() => LaneData;

    public abstract IPreviewer EventPreviewer { get; }
    public abstract IInstrument ParentInstrument { get; }

    /// <summary>
    /// Update the events corresponding to this event's lane.
    /// </summary>
    public abstract void RefreshLane();

    #endregion

    #region CreateEvent

    // This is the one edit-type action that I feel makes the most sense
    // (and is the simplest)
    // to just keep in the Event class.
    public virtual void CreateEvent(int newTick, T newData)
    {
        // All editing of events does not come from adding an event that already exists
        // Do not create event if one already exists at that point in the set
        // If modification is required, user will drag/double click/delete etc.
        // Since creating new event in BPM/TS inherits the last event's properties,
        // Creating the same event twice is a waste of computing power.
        if (LaneData.ContainsKey(newTick))
        {
            Selection.Clear();
            return;
        }
        LaneData.Add(newTick, newData);

        Chart.Refresh();
    }

    #endregion

    #region Selections

    public bool IsPreviewEvent { get; set; } = false;
    public virtual void OnPointerDown(PointerEventData pointerEventData)
    {
        if (IsPreviewEvent) return;

        // used for right click + left click delete functionality
        if (Input.GetMouseButton(RMB_ID) && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            ParentInstrument.DeleteTick(Tick, Lane);
            return;
        }

        CalculateSelectionStatus(pointerEventData);
    }

    public bool CheckForSelection()
    {
        if (Selection.Contains(Tick) && SelectionOverlay != null) return true;
        else return false;
    }

    public static int lastTickSelection;
    /// <summary>
    /// Calculate the event(s) to be selected based on the last click event.
    /// </summary>
    /// <param name="clickButton">PointerEventData.button</param>
    public void CalculateSelectionStatus(PointerEventData clickData) // refactor this pls
    {
        // Goal is to follow standard selection functionality of most productivity programs
        if (clickData.button != PointerEventData.InputButton.Left || !Chart.IsSelectionAllowed()) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            ParentInstrument.ShiftClickSelect(minNum, maxNum);
            Chart.Refresh();
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Selection.Contains(Tick))
            {
                Selection.Remove(Tick);
            }
            else
            {
                Selection.Add(Tick);
            }
            RefreshLane();
        }
        // Regular click, no extra significant keybinds
        else
        {
            if (ParentInstrument.TotalSelectionCount < 2) ParentInstrument.ClearAllSelections();
            Selection.Add(Tick);
            Chart.Refresh();
        }

        // Record the last selection data for shift-click selection
        if (Selection.Contains(Tick)) lastTickSelection = Tick;
    }

    protected static WaitForSeconds clickCooldown = new(doubleClickTime);

    public void AddToSelection()
    {
        Selection.Add(Tick);
    }

    public void RemoveFromSelection()
    {
        Selection.Remove(Tick);
    }

    #endregion
}
