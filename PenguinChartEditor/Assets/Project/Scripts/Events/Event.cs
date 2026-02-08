using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#region Interface

public interface IEvent
{
    int Tick { get; }
    bool Visible { get; set; }
    int Lane { get; }

    ISelection GetSelection();
    ILaneData GetLaneData();
    IInstrument ParentInstrument { get; }

    void InitializeEventAsPreviewer(int tick, IEventData data, ILane parentLane);

    bool IsPreviewEvent { get; set; }

    void AddToSelection();
    void RemoveFromSelection();
}

#endregion

// Useful information about the event data structure/hierarchy:
// Every event is displayed to the user via an in-game game object with its own functionality & visual properties (defined in children).
// Every event has a SpawningLane inheritor that determines when and how to display an event.
// Every event has a Pooler that actually spawns the event.
// Every event has a Previewer that allows the user to place notes.
// Every event has an Instrument that is a monolith for all data & data modification related to that event. 

// Updates and exchange of information is usually made possible by Chart.InPlaceRefresh() (or equivalent "refresh" function, if the name changes)
// Every time a modification of information happens, the outermost trigger (a keybind, move, whatever) triggers a call in an event Instrument
// that performs the requested operation. The chart (display) must then be refreshed. A refresh tells the SpawningLanes to recalculate displayed events.

// Each event (including one tasked with event handler) has an assigned lane 
// Lane assignment happens through lane properties/fields and through GetLaneData() which is a reference to its "instrument" lane data.
// Use the interfaces guaranteed in IEvent above to access necessary functions/properties (add as needed)
public abstract class Event<T> : MonoBehaviour, IEvent, IPoolable, IPointerDownHandler where T : IEventData
{
    #region Constants

    protected const float PREVIEWER_Y_OFFSET = 0.00001f;
    private const float doubleClickTime = 0.3f;
    private const int RMB_ID = 1;
    
    #endregion

    #region IPoolable implementation 

    public Coroutine destructionCoroutine { get; set; }
    
    #endregion

    #region Initialization

    /// <summary>
    /// Called once upon an event's creation. Since an event is always tied to the SpawningLane for which it was created,
    /// ParentLane and its laneID will never change for the lifetime of the event.
    /// </summary>
    public void InitializeProperties(ILane parentLane)
    {
        ParentLane = parentLane;
        Lane = ParentLane.laneID;
    }
    
    public void InitializeEventAsPreviewer(int tick, IEventData data, ILane parentLane)
    {
        Tick = tick;
        representedData = (T)data;
        InitializeProperties(parentLane);
        
        InitializeEventAsPreviewer();
        UpdatePosition();
    }
    protected abstract void InitializeEventAsPreviewer();

    /// <summary>
    /// Initialize event and automatically grab its corresponding data from its event dictionary. 
    /// </summary>
    /// <param name="tick">The tick representing this event</param>
    public void InitializeEvent(int tick) => InitializeEvent(tick, LaneData[tick]);
    
    private void InitializeEvent(int tick, T data)
    {
        Tick = tick;
        representedData = data;
        
        InitializeEvent();
        UpdatePosition();
        
        if (!readOnly) CheckForSelection();
    }

    protected abstract void InitializeEvent();
    protected abstract void UpdatePosition();
    
    #endregion

    #region Location Calculations

    // Chart.instance.SceneDetails.HighwayLength points to the SecretHighway, which is an invisible highway that exists
    // to perform cross-lane movement even when there is no highway to cast to. The length of the SecretHighway is
    // the same as all highways on the scene. If in future you want to have individual highway lengths, change this to reference
    // the parentInstrument's highway3D highway length. Not already doing this because TempoMap uses UI elements which is different
    // and SceneDetails already works out the conversion in 2D. 
    protected float GetDefaultZ() => 
        (float)(Waveform.GetWaveformRatio(Tick) * Chart.instance.SceneDetails.HighwayLength);

    protected float GetSpecifiedZ(int tick) =>
        (float)(Waveform.GetWaveformRatio(tick) * Chart.instance.SceneDetails.HighwayLength);
    
    /// <summary>
    /// Waveform.GetWaveformRatio() deals only with positive cached tick:time ratios for efficiency, so if a negative z
    /// is required (like in the case of spawning sections), this specifies explicit calculation of negative positions.
    /// </summary>
    /// <returns>
    /// The Z coordinate in world space that corresponds to the event's tick, in relation to time t=SongTime.SongPositionSeconds.
    /// </returns>
    protected float GetGuaranteedNegativeZ() =>
        (float)(Waveform.GetWaveformRatio(Tick, true) * Chart.instance.SceneDetails.HighwayLength);
    
    #endregion
    
    #region Properties
    
    public int Tick { get; private set; } = -1;
    
    /// <remarks>
    /// Define as true if the event type is ISustainable. Remember to create a sustain tail object!
    /// </remarks>
    protected abstract bool HasSustainTrail { get; }
    [field: SerializeField] public bool IsPreviewEvent { get; set; } = false;

    // ugh me when "you can't duplicate a serialized field" so you have to make a variable a function
    protected virtual bool HasDoubleClickAction() => false;
    protected virtual void ExecuteDoubleClickAction() {}
    
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

    private bool _selected = false;
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }

    /// <remarks>
    /// Wrapper for gameObject.activeInHierarchy and gameObject.SetActive(). Unity's InputActionMap works even when
    /// game object is not enabled, so this does not interfere with inputs, unlike if events used Update() to poll for
    /// inputs.
    /// </remarks>
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
            _parLane = value;
        }
    }
    private ILane _parLane;

    public T representedData;

    public bool readOnly = false;
    
    public abstract int Lane { get; set; }
    
    #endregion

    #region Data Access

    // These properties point to each event type's instrument data
    // so they can be used in the broad "Event" class.
    // Data is always stored in an "instrument" object accessed through the event's parent GameInstrument or Chart
    public abstract SelectionSet<T> Selection { get; }
    public ISelection GetSelection() => Selection;

    protected abstract LaneSet<T> LaneData { get; }
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
    }

    #endregion

    #region Selections

    private int clickCount = 0;
    
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

        // Sustain management/activation
        if (HasSustainTrail && pointerEventData.button == PointerEventData.InputButton.Right)
        {
            if (Input.GetKey(KeyCode.LeftShift) || !UserSettings.ExtSustains)
            {
                ParentInstrument.ShiftClickSelect(Tick);
                return;
            }
            Selection.Add(Tick);
            Chart.InPlaceRefresh();
        }

        if (!HasDoubleClickAction()) return;
        
        if (pointerEventData.button != PointerEventData.InputButton.Left || !LaneData.ContainsKey(Tick)) return;
        
        clickCount++;

        // Double click functionality for manual entry of any info type - beatlines, sections
        // eventData.clickCount does not work here - pointerDown and pointerUp do not trigger click count for some reason
        // so manual coroutine solution is here to circumvent that issue
        if (!Input.GetKey(KeyCode.LeftControl) && // explicitly for bpm label editing
            Chart.IsModificationAllowed() && 
            clickCount >= 2)
        {
            ExecuteDoubleClickAction();

            // if you click too fast, clickCount will exceed 2
            // at some point and will never be able to reset
            // reset here to avoid arbitrarily bricked label object
            clickCount = 0;
        }

        if (clickCount == 1 && Visible) StartCoroutine(TriggerDoubleClick());
    }

    private IEnumerator TriggerDoubleClick()
    {
        yield return clickCooldown;
        clickCount = 0;
    }

    private void CheckForSelection()
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

    private static int lastTickSelection;

    // If you've ended up here because selections aren't working during testing, here are some common fixes:
    // - Check to make sure you're in selection mode
    // - Check to make sure the object has a collider
    // - Check to make sure the object is on the "Notes" layer in the inspector 
    // - Check to make sure the Main Camera has a Physics Raycaster and that it is set up to detect ChartingHighway, Notes, and WorldButtons
    // Can you tell I've had lots of issues with this? Because I have and it's one of these four every hecking time
    // (hecking because it's not that big of a deal lmao)
    // -Emperor
    protected void CalculateSelectionStatus(PointerEventData clickData) // refactor this pls
    {
        // Goal is to follow standard selection functionality of most productivity programs
        if (
            clickData.button != PointerEventData.InputButton.Left 
            || !Chart.IsSelectionAllowed() 
            || Chart.LoadedInstrument != ParentInstrument
            ) 
            return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            var minNum = Math.Min(lastTickSelection, Tick);
            var maxNum = Math.Max(lastTickSelection, Tick);
            if (Input.GetKey(KeyCode.LeftControl))
            {
                ParentInstrument.ShiftClickSelectLane(minNum, maxNum, Lane);
            }
            else
            {
                ParentInstrument.ShiftClickSelect(minNum, maxNum);
            }
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

    public void AddToSelection() => Selection.Add(Tick);
    public void RemoveFromSelection() => Selection.Remove(Tick);

    #endregion
}
