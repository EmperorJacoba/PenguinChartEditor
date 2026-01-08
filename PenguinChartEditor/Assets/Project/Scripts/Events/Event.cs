using System;
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

    bool IsPreviewEvent { get; set; }
}

#endregion

// Each event (including one tasked with event handler) has an assigned lane 
// Lane assignment happens through lane properties/fields and through GetLaneData() which is just a reference to its "instrument" lane data.
// Use the interfaces guaranteed in IEvent above to access necessary functions/properties (add as needed)
public abstract class Event<T> : MonoBehaviour, IEvent, IPointerDownHandler where T : IEventData
{
    public bool readOnly = false;
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
    protected int _tick = -1;

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
            if (Visible != value) gameObject.SetActive(value);
        }
    }

    public ILane ParentLane
    {
        get
        {
            _parLane ??= GetComponentInParent<ILane>();
            return _parLane;
        }
        set
        {
            if (_parLane == value) return;
            _parLane = value;
        }
    }
    ILane _parLane;

    public T representedData;

    #endregion

    #region Data Access

    // These properties point to each event type's instrument data
    // so they can be used in the broad "Event" class.
    // Data is always stored in an instrument, or in the BPM/TS case, in the Tempo/TimeSignature classes.
    public abstract SelectionSet<T> Selection { get; }
    public ISelection GetSelection() => Selection;

    public abstract LaneSet<T> LaneData { get; }
    public ILaneData GetLaneData() => LaneData;

    public abstract IInstrument ParentInstrument { get; }

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
        if (LaneData.ContainsKey(newTick))
        {
            Selection.Clear();
            return;
        }
        LaneData.Add(newTick, newData);

        Chart.InPlaceRefresh();
    }

    #endregion

    #region Selections

    public bool IsPreviewEvent { get; set; } = false;
    public virtual void OnPointerDown(PointerEventData pointerEventData)
    {
        if (IsPreviewEvent || readOnly) return;

        // used for right click + left click delete functionality
        if (Input.GetMouseButton(RMB_ID) && pointerEventData.button == PointerEventData.InputButton.Left)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                ParentInstrument.DeleteAllEventsAtTick(Tick);
            }
            ParentInstrument.DeleteTickInLane(Tick, Lane);
            return;
        }

        CalculateSelectionStatus(pointerEventData);
    }

    public void CheckForSelection()
    {
        if (SelectionOverlay != null && Selection.Contains(Tick))
        {
            Selected = true;
        }
        else 
        {
            Selected = false;
        }
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
        }
        // Regular click, no extra significant keybinds
        else
        {
            if (!Selection.Contains(Tick))
            {
                ParentInstrument.ClearAllSelections();
            }
            Selection.Add(Tick);
        }
        Chart.InPlaceRefresh();

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
