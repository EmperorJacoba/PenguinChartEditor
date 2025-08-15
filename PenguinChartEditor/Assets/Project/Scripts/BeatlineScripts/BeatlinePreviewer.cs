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
    [SerializeField] RectTransform boundaryReference;

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

    public static BeatlinePreviewer instance;

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position => UpdatePreviewPosition(position.ReadValue<Vector2>().y / Screen.height, position.ReadValue<Vector2>().x / Screen.width);
        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();

        // Preview also needs to update when waveform moves
        WaveformManager.DisplayChanged += UpdatePreviewPosition;

        instance = this;
    }

    void Start()
    {
        Type = Beatline.BeatlineType.none;
        tsLabel.Visible = false;
        bpmLabel.Visible = false;
    }

    public static bool justCreated = false;
    void Update()
    {
        if (justCreated) justCreated = false;
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

        Tick = SongTimelineManager.CalculateGridSnappedTick(percentOfScreenVertical);

        WaveformManager.GetCurrentDisplayedWaveformInfo(out var startTick, out var endTick, out var timeShown, out var startTime, out var endTime);

        // store what time the preview is at so that adding an event is merely inserting the preview's current position into the dictionary
        timestamp = BPM.ConvertTickTimeToSeconds(Tick);
        UpdateBeatlinePosition((timestamp - startTime) / timeShown, boundaryReference.rect.height);

        // store the current previewed Tick for copy/pasting selections from the preview onward
        currentPreviewTick = Tick;

        // preview TS event or BPM event based on what side of the track cursor is on (track is centered)
        if (percentOfScreenHorizontal < 0.5f)
        {
            bpmLabel.Visible = false;
            tsLabel.Visible = true;

            var num = TimeSignature.EventData.Events[TimeSignature.GetLastTSEventTick(Tick)].Numerator;
            var denom = TimeSignature.EventData.Events[TimeSignature.GetLastTSEventTick(Tick)].Denominator;
            tsLabel.LabelText = $"{num} / {denom}";
            displayedTS = new(num, denom);
        }
        else
        {
            bpmLabel.Visible = true;
            tsLabel.Visible = false;
            bpmLabel.LabelText = BPM.EventData.Events[BPM.GetLastTempoEventTickInclusive(Tick)].BPMChange.ToString();
        }
    }

    /// <summary>
    /// Add the currently previewed event to the real dictionary.
    /// </summary>
    void CreateEvent()
    {
        // No creating events while user is messing with other UI parts (volume, etc.)
        if (IsOverlayRaycasterHit()) return;

        if (bpmLabel.Visible && !bpmLabel.GetEventData().Events.ContainsKey(Tick))
        {
            bpmLabel.CreateEvent(Tick, new BPMData(float.Parse(bpmLabel.LabelText), (float)timestamp));
        }
        else if (tsLabel.Visible && !tsLabel.GetEventData().Events.ContainsKey(Tick))
        {
            tsLabel.CreateEvent(Tick, displayedTS);
        }
        else return;

        justCreated = true;
        // Show changes to user
        TempoManager.UpdateBeatlines();
    }

    public bool IsOverlayRaycasterHit()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        overlayUIRaycaster.Raycast(pointerData, results);

        // If a component from the toolboxes is raycasted from the cursor, then the overlay is hit.
        if (results.Count > 0) return true; else return false;
    }
}
