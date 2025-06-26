using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Script attached to the object that "previews" a potential event change. This object is configured like a beatline.
/// </summary>
public class BeatlinePreviewer : MonoBehaviour
{
    /// <summary>
    /// A reference to the underlying beatline game object this script is attached to.
    /// </summary>
    [SerializeField] Beatline beatline;
    [SerializeField] RectTransform screenReferenceRt;

    [SerializeField] GraphicRaycaster overlayUIRaycaster;

    static InputMap inputMap;

    public static int currentPreviewTick;

    /// <summary>
    /// Temporary solution to prevent editing capabilities in some circumstances
    /// </summary>
    public static bool editMode = true;

    /// <summary>
    /// Holds the tick position of the preview beatline.
    /// </summary>
    int tick = 0;

    /// <summary>
    /// Holds the time position of the preview beatline.
    /// </summary>
    double timestamp = 0;

    /// <summary>
    /// Holds
    /// </summary>
    (int, int) displayedTS = (4, 4);

    /// <summary>
    /// Which input field is targeted by the focused tick for editing?
    /// </summary>
    public enum PreviewType
    {
        none = 0,
        BPM = 1,
        TS = 2
    }

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position => UpdatePreviewPosition(position.ReadValue<Vector2>().y / screenReferenceRt.rect.height, position.ReadValue<Vector2>().x / screenReferenceRt.rect.width);
        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();

        // Preview also needs to update when waveform moves
        WaveformManager.DisplayChanged += UpdatePreviewPosition;
    }

    void Start()
    {
        beatline.Type = Beatline.BeatlineType.none;
        beatline.BPMLabelVisible = false;
        beatline.TSLabelVisible = false;
    }

    /// <summary>
    /// Shortcut to allow void events call the main UpdatePreviewPosition function.
    /// </summary>
    void UpdatePreviewPosition()
    {
        UpdatePreviewPosition(Input.mousePosition.y / screenReferenceRt.rect.height, Input.mousePosition.x / screenReferenceRt.rect.width);
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
            beatline.TSLabelVisible = false;
            beatline.BPMLabelVisible = false;
            return;
        }

        WaveformManager.GetCurrentDisplayedWaveformInfo(out var startTick, out var endTick, out var timeShown, out var startTime, out var endTime);

        var cursorTimestamp = (percentOfScreenVertical * timeShown) + startTime;
        var cursorTickTime = SongTimelineManager.ConvertSecondsToTickTime((float)cursorTimestamp); 

        // Calculate the tick grid to snap the event to
        var tickInterval = ChartMetadata.ChartResolution / ((float)DivisionChanger.CurrentDivision / 4);

        // Calculate the cursor's tick position in the context of the origin of the grid (last barline) 
        var divisionBasisTick = cursorTickTime - SongTimelineManager.FindLastBarline(cursorTickTime);

        // Find how many ticks off the cursor position is from the grid 
        var remainder = divisionBasisTick % tickInterval;

        // Remainder will show how many ticks off from the last event we are
        // Use remainder to determine which grid snap we are closest to and round to that
        if (remainder > (tickInterval / 2)) // Closer to following snap
        {
            // Regress to last grid snap and then add a snap to get to next grid position
            tick = (int)Math.Floor(cursorTickTime - remainder + tickInterval);
        }
        else // Closer to previous grid snap or dead on a snap (subtract 0 = no change)
        {
            // Regress to last grid snap
            tick = (int)Math.Floor(cursorTickTime - remainder);
        }

        // store what time the preview is at so that adding an event is merely inserting the preview's current position into the dictionary
        timestamp = SongTimelineManager.ConvertTickTimeToSeconds(tick);
        beatline.UpdateBeatlinePosition((timestamp - startTime) / timeShown);

        // store the current previewed tick for copy/pasting selections from the preview onward
        currentPreviewTick = tick;

        // preview TS event or BPM event based on what side of the track cursor is on (track is centered)
        if (percentOfScreenHorizontal < 0.5f)
        {
            beatline.BPMLabelVisible = false;
            beatline.TSLabelVisible = true;

            var num = SongTimelineManager.TimeSignatureEvents[SongTimelineManager.FindLastTSEventTick(tick)].Item1;
            var denom = SongTimelineManager.TimeSignatureEvents[SongTimelineManager.FindLastTSEventTick(tick)].Item2;
            beatline.TSLabelText = $"{num} / {denom}";
            displayedTS = (num, denom);
        }
        else
        {
            beatline.BPMLabelVisible = true;
            beatline.TSLabelVisible = false;
            beatline.BPMLabelText = SongTimelineManager.TempoEvents[SongTimelineManager.FindLastTempoEventTickInclusive(tick)].Item1.ToString();
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
        if (beatline.BPMLabelVisible)
        {
            if (!SongTimelineManager.TempoEvents.ContainsKey(tick))
            {
                SongTimelineManager.TempoEvents.Add(tick, (float.Parse(beatline.BPMLabelText), (float)timestamp));
                BeatlineSelectionManager.SelectedBPMTicks.Clear();
                BeatlineSelectionManager.SelectedTSTicks.Clear();
            }
        }
        else if (beatline.TSLabelVisible)
        {
            if (!SongTimelineManager.TimeSignatureEvents.ContainsKey(tick))
            {
                SongTimelineManager.TimeSignatureEvents.Add(tick, displayedTS);
                BeatlineSelectionManager.SelectedBPMTicks.Clear();
                BeatlineSelectionManager.SelectedTSTicks.Clear();
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
