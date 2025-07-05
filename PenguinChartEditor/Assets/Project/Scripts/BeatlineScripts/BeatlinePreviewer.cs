using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Script attached to the object that "previews" a potential event change. This object is configured like a beatline.
/// </summary>
public class BeatlinePreviewer : Beatline
{
    [SerializeField] GraphicRaycaster overlayUIRaycaster;

    public static int currentPreviewTick;

    /// <summary>
    /// Temporary solution to prevent editing capabilities in some circumstances
    /// </summary>
    public static bool editMode = true;

    /// <summary>
    /// Holds the time position of the preview beatline.
    /// </summary>
    double timestamp = 0;

    /// <summary>
    /// Holds
    /// </summary>
    TSData displayedTS = new(4, 4);

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position => UpdatePreviewPosition(position.ReadValue<Vector2>().y / Screen.height, position.ReadValue<Vector2>().x / Screen.width);
        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();

        // Preview also needs to update when waveform moves
        WaveformManager.DisplayChanged += UpdatePreviewPosition;
    }

    void Start()
    {
        Type = Beatline.BeatlineType.none;
        tsLabel.Visible = false;
        bpmLabel.Visible = false;
    }

    /// <summary>
    /// Shortcut to allow void events call the main UpdatePreviewPosition function.
    /// </summary>
    void UpdatePreviewPosition()
    {
        UpdatePreviewPosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);
    }

    /// <summary>
    /// Update the position of the beatline preview to lock to a certain part of the screen
    /// </summary>
    /// <param name="percentOfScreenVertical">The percent up from the bottom of the screen that the target is at (mouse cursor).</param>
    /// <param name="percentOfScreenHorizontal">The percent right from the left of the screen that the target is at (mouse cursor).</param>
    void UpdatePreviewPosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        // Check to make sure editing is allowed and that a negative percent isn't passed in
        if (!editMode || percentOfScreenVertical < 0 || percentOfScreenHorizontal < 0) return;

        // Check to make sure user isn't doing something else atm (like adjusting volume, etc.)
        // In which case, disable visibility of the preview.
        if (IsOverlayRaycasterHit())
        {
            tsLabel.Visible = false;
            bpmLabel.Visible = false;
            return;
        }

        WaveformManager.GetCurrentDisplayedWaveformInfo(out var startTick, out var endTick, out var timeShown, out var startTime, out var endTime);

        var cursorTimestamp = (percentOfScreenVertical * timeShown) + startTime;
        var cursorTickTime = BPM.ConvertSecondsToTickTime((float)cursorTimestamp); 

        // Calculate the Tick grid to snap the event to
        var TickInterval = ChartMetadata.ChartResolution / ((float)DivisionChanger.CurrentDivision / 4);

        // Calculate the cursor's Tick position in the context of the origin of the grid (last barline) 
        var divisionBasisTick = cursorTickTime - TimeSignature.FindLastBarline(cursorTickTime);

        // Find how many Ticks off the cursor position is from the grid 
        var remainder = divisionBasisTick % TickInterval;

        // Remainder will show how many Ticks off from the last event we are
        // Use remainder to determine which grid snap we are closest to and round to that
        if (remainder > (TickInterval / 2)) // Closer to following snap
        {
            // Regress to last grid snap and then add a snap to get to next grid position
            Tick = (int)Math.Floor(cursorTickTime - remainder + TickInterval);
        }
        else // Closer to previous grid snap or dead on a snap (subtract 0 = no change)
        {
            // Regress to last grid snap
            Tick = (int)Math.Floor(cursorTickTime - remainder);
        }

        // store what time the preview is at so that adding an event is merely inserting the preview's current position into the dictionary
        timestamp = BPM.ConvertTickTimeToSeconds(Tick);
        UpdateBeatlinePosition((timestamp - startTime) / timeShown);

        // store the current previewed Tick for copy/pasting selections from the preview onward
        currentPreviewTick = Tick;

        // preview TS event or BPM event based on what side of the track cursor is on (track is centered)
        if (percentOfScreenHorizontal < 0.5f)
        {
            bpmLabel.Visible = false;
            tsLabel.Visible = true;

            var num = TimeSignature.Events[TimeSignature.FindLastTSEventTick(Tick)].Numerator;
            var denom = TimeSignature.Events[TimeSignature.FindLastTSEventTick(Tick)].Denominator;
            tsLabel.LabelText = $"{num} / {denom}";
            displayedTS = new(num, denom);
        }
        else
        {
            bpmLabel.Visible = true;
            tsLabel.Visible = false;
            bpmLabel.LabelText = BPM.Events[BPM.FindLastTempoEventTickInclusive(Tick)].BPMChange.ToString();
        }
    }

    /// <summary>
    /// Add the currently previewed event to the real dictionary.
    /// </summary>
    void CreateEvent()
    {
        // No creating events while user is messing with other UI parts (volume, etc.)
        if (IsOverlayRaycasterHit()) return;

        // Modify the dictionaries based on which event change is previewed
        if (bpmLabel.Visible)
        {
            if (!BPM.Events.ContainsKey(Tick))
            {
                BPM.Events.Add(Tick, new BPMData(float.Parse(bpmLabel.LabelText), (float)timestamp));
                BPM.SelectedBPMEvents.Clear();
                TimeSignature.SelectedTSEvents.Clear(); // clear selection generic? attack all children?
            }
        }
        else if (tsLabel.Visible)
        {
            if (!TimeSignature.Events.ContainsKey(Tick))
            {
                TimeSignature.Events.Add(Tick, displayedTS);
                BPM.SelectedBPMEvents.Clear();
                TimeSignature.SelectedTSEvents.Clear();
            }
        }
        else return;

        // Show changes to user
        TempoManager.UpdateBeatlines();
    }

    bool IsOverlayRaycasterHit()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        overlayUIRaycaster.Raycast(pointerData, results);

        // If a component from the toolboxes is raycasted from the cursor, then the overlay is hit.
        if (results.Count > 0) return true; else return false;
    }
}
