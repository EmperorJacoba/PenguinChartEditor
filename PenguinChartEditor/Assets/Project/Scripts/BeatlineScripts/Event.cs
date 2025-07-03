using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

public abstract class Event<DataType> : MonoBehaviour, IEvent<DataType>
{
    protected InputMap inputMap;
    public int Tick { get; set; }
    public abstract HashSet<int> GetSelectedEvents();
    public abstract SortedDictionary<int, DataType> GetEventClipboard();
    public abstract SortedDictionary<int, DataType> GetTargetEventSet();

    public void CopySelection()
    {
        var clipboard = GetEventClipboard();
        var selection = GetSelectedEvents();
        var targetEventSet = GetTargetEventSet();

        clipboard.Clear();

        int lowestTick = 0;
        if (selection.Count > 0) lowestTick = selection.Min();

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
    }

    public virtual void PasteSelection()
    {
        var startPasteTick = BeatlinePreviewer.currentPreviewTick;

        var clipboard = GetEventClipboard();
        var targetEventSet = GetTargetEventSet();

        if (clipboard.Count > 0)
        {
            var endPasteTick = clipboard.Keys.Max() + startPasteTick;
            SortedDictionary<int, DataType> tempDict = GetNonOverwritableDictEvents(targetEventSet, startPasteTick, endPasteTick);

            foreach (var clippedTick in clipboard)
            {
                tempDict.Add(clippedTick.Key + startPasteTick, clipboard[clippedTick.Key]);
            }
            targetEventSet = tempDict;
        }

        TempoManager.UpdateBeatlines();
    }

    public virtual void DeleteSelection()
    {
        var selection = GetSelectedEvents();
        var targetEventSet = GetTargetEventSet();

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

        TempoManager.UpdateBeatlines();
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

    public bool DeletePrimed { get; set; }

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

public interface IEvent<DataType>
{
    HashSet<int> GetSelectedEvents();
    public int Tick { get; set; }
    public bool Selected { get; set; }
    public GameObject SelectionOverlay { get; set; }
    public bool Visible { get; set; }
    public bool DeletePrimed { get; set; }

    void HandlePointerDown(BaseEventData baseEventData);
    void HandlePointerUp(BaseEventData baseEventData);
    public SortedDictionary<int, DataType> GetEventClipboard();

    public void CopySelection();
    public void PasteSelection();
    public void DeleteSelection();
    public SortedDictionary<int, DataType> GetTargetEventSet();
}