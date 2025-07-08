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

    SortedDictionary<int, DataType> GetEventClipboard();
    
    HashSet<int> GetSelectedEvents();

    void CopySelection();
    void PasteSelection();
    void DeleteSelection();

    // Get and set functions are required for common abstract functions (ex. copy/paste)
    SortedDictionary<int, DataType> GetEvents();
    void SetEvents(SortedDictionary<int, DataType> newEvents);

}

public abstract class Event<DataType> : MonoBehaviour, IEvent<DataType>
{
    protected InputMap inputMap;
    public int Tick { get; set; }
    public abstract HashSet<int> GetSelectedEvents();
    public abstract SortedDictionary<int, DataType> GetEventClipboard();
    public abstract SortedDictionary<int, DataType> GetEvents();
    public abstract void SetEvents(SortedDictionary<int, DataType> newEvents);
    [field: SerializeField] public GameObject SelectionOverlay { get; set; }
    public bool DeletePrimed { get; set; } // make global across events 

    /*
        public void CopySelection()
        {
            var clipboard = GetEventClipboard();
            var selection = GetSelectedEvents();
            var targetEventSet = GetEvents();

            clipboard.Clear(); // prep dictionary for new copy data

            // copy data is shifted to zero for relative pasting 
            // (ex. an event sequence 100, 200, 300 is converted to 0, 100, 200)
            int lowestTick = 0;
            if (selection.Count > 0) lowestTick = selection.Min();

            // add relevant data for each tick into clipboard
            foreach (var selectedTick in selection)
            {
                try
                {
                    clipboard.Add(selectedTick - lowestTick, targetEventSet[selectedTick]);
                }
                catch
                {
                    continue;
                }
            }
        } */

    public void CopySelection()
    {
        var copyAction = new Copy<DataType>();
        copyAction.Execute(GetEventClipboard(), GetSelectedEvents(), GetEvents());
    }

    public virtual void PasteSelection()
    {
        var startPasteTick = BeatlinePreviewer.currentPreviewTick;
        var clipboard = GetEventClipboard();
        var targetEventSet = GetEvents();
    
        if (clipboard.Count > 0) // avoid index error
        {
            // Create a temp dictionary without events within the size of the clipboard from the origin of the paste 
            // (ex. clipboard with 0, 100, 400 has a zone of 400, paste starts at tick 700, all events tick 700-1100 are wiped)
            var endPasteTick = clipboard.Keys.Max() + startPasteTick;
            SortedDictionary<int, DataType> tempDict = GetNonOverwritableDictEvents(targetEventSet, startPasteTick, endPasteTick);

            // Add clipboard data to temp dict, now cleaned of obstructing events
            foreach (var clippedTick in clipboard)
            {
                tempDict.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
            }
            // Commit the temporary dictionary to the real dictionary
            // (cannot use targetEventSet as that results with a local reassignment)

            SetEvents(tempDict);
        }

        TempoManager.UpdateBeatlines();
    }

    public void CutSelection()
    {
        CopySelection();
        DeleteSelection();
    }

    /// <summary>
    /// Copy all events from an event dictionary that are not within a paste zone (startTick to endTick)
    /// </summary>
    /// <typeparam name="Key">Key is a tick (int)</typeparam>
    /// <typeparam name="DataType">Event data -> ex. TS: (int, int)</typeparam>
    /// <param name="originalDict">Target dictionary of events to extract from.</param>
    /// <param name="startTick">Start of paste zone (events to overwrite)</param>
    /// <param name="endTick">End of paste zone (events to overwrite)</param>
    /// <returns>Event dictionary with all events in the paste zone removed.</returns>
    protected SortedDictionary<int, DataType> GetNonOverwritableDictEvents(SortedDictionary<int, DataType> originalDict, int startTick, int endTick)
    {
        SortedDictionary<int, DataType> tempDictionary = new();
        foreach (var item in originalDict)
        {
            if (item.Key < startTick || item.Key > endTick) tempDictionary.Add(item.Key, item.Value);
        }
        return tempDictionary;
    }

    public void DeleteSelection()
    {
        var selection = GetSelectedEvents();

        // This makes a copy so that it works for BPM along with other data types
        var targetEventSet = new SortedDictionary<int, DataType>(GetEvents());

        if (selection.Count != 0)
        {
            foreach (var tick in selection)
            {
                if (tick != 0)
                {
                    targetEventSet.Remove(tick);
                }
            }
        }
        selection.Clear();

        // BPM dict needs to be recalculated after every modification
        // So copy + overwrite with new dictionary is needed to cleanly fit into this system
        // (BPM.SetEvents() recalculates the event dictionary timestamps every time it is used)
        SetEvents(targetEventSet); // maybe augment .Add/remove? for create event?

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

    public int lastTickSelection;
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

    /// <summary>
    /// Add the currently previewed event to the real dictionary.
    /// </summary>
    public void CreateEvent(int newTick, DataType newData)
    {
        var eventDict = new SortedDictionary<int, DataType>(GetEvents());

        eventDict.Remove(newTick);
        eventDict.Add(newTick, newData);
        GetSelectedEvents().Clear(); // global clear dict?

        SetEvents(eventDict);

        // Show changes to user
        TempoManager.UpdateBeatlines();
    }
}
