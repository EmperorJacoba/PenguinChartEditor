using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System;
using System.Net;

public class BeatlineSelectionManager : MonoBehaviour
{
    static InputMap inputMap;
    /// <summary>
    /// Holds a reference for all the BPM events currently selected.
    /// </summary>
    public static HashSet<int> SelectedBPMTicks { get; set; } = new();

    /// <summary>
    /// Holds a reference for all the TS events currently selected.
    /// </summary>
    public static HashSet<int> SelectedTSTicks { get; set; } = new();

    /// <summary>
    /// Holds the tick that was last interacted with in a selection. Used for shift-click capabilities.
    /// </summary>
    public static int lastTickSelection = 0;

    public static SortedDictionary<int, float> bpmClipboard = new();
    public static SortedDictionary<int, (int, int)> tsClipboard = new();

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.Delete.performed += x => DeleteSelection();
        inputMap.Charting.Copy.performed += x => CopySelection();
        inputMap.Charting.Paste.performed += x => PasteSelection();
    }
    void CopySelection()
    {
        int lowestBPMTick = 0;
        int lowestTSTick = 0;
        if (SelectedBPMTicks.Count > 0) lowestBPMTick = SelectedBPMTicks.Min();
        if (SelectedTSTicks.Count > 0) lowestTSTick = SelectedTSTicks.Min();

        foreach (var tick in SelectedBPMTicks)
        {
            try
            {
                bpmClipboard.Add(tick - lowestBPMTick, SongTimelineManager.TempoEvents[tick].Item1);
            }
            catch
            {
                continue;
            }
        }
        foreach (var tick in SelectedTSTicks)
        {
            tsClipboard.Add(tick - lowestTSTick, SongTimelineManager.TimeSignatureEvents[tick]);
        }
    }

    void PasteSelection()
    {
        var startPasteTick = BeatlinePreviewer.currentPreviewTick;

        // This works because the event does end up in the target dictionary, but event dictionary events get all screwed up
        // and overwrite each other (???)
        // Check dictionaries to see what is actually outputted
        if (bpmClipboard.Count > 0)
        {
            var endBPMPasteTick = bpmClipboard.Keys.Max() + startPasteTick;
            SortedDictionary<int, (float, float)> tempTempoEventDictionary = GetNonOverwritableDictEvents(SongTimelineManager.TempoEvents, startPasteTick, endBPMPasteTick);
            foreach (var tick in bpmClipboard)
            {
                Debug.Log($"{tick.Key + startPasteTick}, {tick.Value}");
                tempTempoEventDictionary.Add(tick.Key + startPasteTick, (bpmClipboard[tick.Key], 0));
            }
            SongTimelineManager.TempoEvents = tempTempoEventDictionary;
            SongTimelineManager.RecalculateTempoEventDictionary(startPasteTick);
        }
        if (tsClipboard.Count > 0)
        {
            var endTSPasteTick = tsClipboard.Keys.Max() + startPasteTick;
            SortedDictionary<int, (int, int)> tempTSEventDictionary = GetNonOverwritableDictEvents(SongTimelineManager.TimeSignatureEvents, startPasteTick, endTSPasteTick);
            foreach (var tick in tsClipboard)
            {
                tempTSEventDictionary.Add(tick.Key, tsClipboard[tick.Key]);
            }
            SongTimelineManager.TimeSignatureEvents = tempTSEventDictionary;
        }

        TempoManager.UpdateBeatlines();
    }

    /// <summary>
    /// Copy all events from an event dictionary that are not within a paste zone (startTick to endTick)
    /// </summary>
    /// <typeparam name="Key">Key is a tick (int)</typeparam>
    /// <typeparam name="Value">Value is event data -> ex. TS: (int, int)</typeparam>
    /// <param name="originalDict">Target dictionary of events to extract from.</param>
    /// <param name="startTick">Start of paste zone (events to overwrite)</param>
    /// <param name="endTick">End of paste zone (events to overwrite)</param>
    /// <returns>Event dictionary with all events in the paste zone removed.</returns>
    SortedDictionary<Key, Value> GetNonOverwritableDictEvents<Key, Value>(SortedDictionary<Key, Value> originalDict, int startTick, int endTick) where Key : IComparable<Key>
    {
        SortedDictionary<Key, Value> tempDictionary = new();
        foreach (var item in originalDict)
        {
            var keyAsInt = Convert.ToInt32(item.Key);
            if (keyAsInt < startTick || keyAsInt > endTick) tempDictionary.Add(item.Key, item.Value);
        }
        return tempDictionary;
    }

    /// <summary>
    /// Calculate the event(s) to be selected based on the last click event.
    /// </summary>
    /// <param name="clickButton">PointerEventData.button</param>
    /// <param name="targetSelectionSet">The selection hash set that contains this event type's selection data.</param>
    /// <param name="targetEventSet">The keys of a sorted dictionary that holds event data (beatlines, TS, etc)</param>
    public static void CalculateSelectionStatus(PointerEventData.InputButton clickButton, HashSet<int> targetSelectionSet, List<int> targetEventSet, int HeldTick)
    {
        // Goal is to follow standard selection functionality of most productivity programs
        if (clickButton != PointerEventData.InputButton.Left) return;

        // Shift-click functionality
        if (Input.GetKey(KeyCode.LeftShift))
        {
            SelectedBPMTicks.Clear();
            SelectedBPMTicks.Clear();

            var minNum = Math.Min(lastTickSelection, HeldTick);
            var maxNum = Math.Max(lastTickSelection, HeldTick);
            HashSet<int> selectedEvents = targetEventSet.Where(x => x <= maxNum && x >= minNum).ToHashSet();
            targetSelectionSet.UnionWith(selectedEvents);
        }
        // Left control if item is already selected
        else if (Input.GetKey(KeyCode.LeftControl) && targetSelectionSet.Contains(HeldTick))
        {
            targetSelectionSet.Remove(HeldTick);
            return; // prevent lastTickSelection from being stored as an unselected number
        }
        // Left control if item is not currently selected
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetSelectionSet.Add(HeldTick);
        }
        // Regular click, no extra significant keybinds
        else
        {
            SelectedBPMTicks.Clear();
            SelectedTSTicks.Clear();
            targetSelectionSet.Add(HeldTick);
        }
        // Record the last selection data for shift-click selection
        lastTickSelection = HeldTick;
    }

    public static void DeleteSelection()
    {
        if (SelectedBPMTicks.Count != 0)
        {
            var earliestTick = SelectedBPMTicks.Min();
            foreach (var tick in SelectedBPMTicks)
            {
                if (tick != 0)
                {
                    SongTimelineManager.TempoEvents.Remove(tick);
                }
            }
            SongTimelineManager.RecalculateTempoEventDictionary(SongTimelineManager.FindLastTempoEventTickInclusive(earliestTick));
        }
        if (SelectedTSTicks.Count != 0)
        {
            foreach (var tick in SelectedTSTicks)
            {
                if (tick != 0)
                {
                    SongTimelineManager.TimeSignatureEvents.Remove(tick);
                }
            }
        }
        SelectedBPMTicks.Clear();
        SelectedTSTicks.Clear();

        TempoManager.UpdateBeatlines();
    }
    
    public static bool CheckForBPMSelected(int heldTick)
    {
        if (SelectedBPMTicks.Contains(heldTick)) return true;
        else return false;
    }

    public static bool CheckForTSSelected(int heldTick)
    {
        if (SelectedTSTicks.Contains(heldTick)) return true;
        else return false;
    }
}