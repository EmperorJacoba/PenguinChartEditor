using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BeatlinePreviewer : MonoBehaviour
{
    [SerializeField] WaveformManager waveformManager;
    [SerializeField] Beatline beatline;
    [SerializeField] RectTransform screenReferenceRt;

    [SerializeField] GraphicRaycaster overlayUIRaycaster;

    static InputMap inputMap;

    public static bool editMode = true;

    int displayedTick = 0;
    double displayedTime = 0;

    (int, int) heldTS = (4, 4);

    public static (int, PreviewType) focusedTick;

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

        WaveformManager.DisplayChanged += UpdatePreviewPosition;
    }

    void Start()
    {
        beatline.Type = Beatline.BeatlineType.none;
        beatline.BPMLabelVisible = false;
        beatline.TSLabelVisible = false;
    }

    void UpdatePreviewPosition()
    {
        UpdatePreviewPosition(Input.mousePosition.y / screenReferenceRt.rect.height, Input.mousePosition.x / screenReferenceRt.rect.width);
    }

    void UpdatePreviewPosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!editMode || percentOfScreenVertical < 0 || percentOfScreenHorizontal < 0) return;
        if (IsOverlayRaycasterHit())
        {
            beatline.TSLabelVisible = false;
            beatline.BPMLabelVisible = false;
            return;
        }

        waveformManager.GetCurrentDisplayedWaveformInfo(out var startTick, out var endTick, out var timeShown, out var startTime, out var endTime);

        var cursorTimestamp = (percentOfScreenVertical * timeShown) + startTime;
        var cursorTickTime = SongTimelineManager.ConvertSecondsToTickTime((float)cursorTimestamp); 

        // Calculate the grid to snap the event to
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
            displayedTick = (int)Math.Floor(cursorTickTime - remainder + tickInterval);
        }
        else // Closer to previous grid snap or dead on a snap (subtract 0 = no change)
        {
            // Regress to last grid snap
            displayedTick = (int)Math.Floor(cursorTickTime - remainder);
        }

        // store what time the preview is at so that adding an event is merely inserting the current position into the dictionary
        displayedTime = SongTimelineManager.ConvertTickTimeToSeconds(displayedTick);
        beatline.UpdateBeatlinePosition((displayedTime - startTime) / timeShown);

        // preview TS event or BPM event based on what side of the track cursor is on (track is centered)
        if (percentOfScreenHorizontal < 0.5f)
        {
            beatline.BPMLabelVisible = false;
            beatline.TSLabelVisible = true;

            var num = SongTimelineManager.TimeSignatureEvents[SongTimelineManager.FindLastTSEventTick(displayedTick)].Item1;
            var denom = SongTimelineManager.TimeSignatureEvents[SongTimelineManager.FindLastTSEventTick(displayedTick)].Item2;
            beatline.TSLabelText = $"{num} / {denom}";
            heldTS = (num, denom);
        }
        else
        {
            beatline.BPMLabelVisible = true;
            beatline.TSLabelVisible = false;
            beatline.BPMLabelText = SongTimelineManager.TempoEvents[SongTimelineManager.FindLastTempoEventTick(displayedTick)].Item1.ToString();
        }
        // get current cursor position
        // calculate the timestamp and following tick time
        // use current division to round that tick time to a current tick
        // place this beatline at that ticktime
    }

    void CreateEvent()
    {
        if (IsOverlayRaycasterHit()) return;

        if (beatline.BPMLabelVisible)
        {
            if (!SongTimelineManager.TempoEvents.ContainsKey(displayedTick))
            {
                SongTimelineManager.TempoEvents.Add(displayedTick, (float.Parse(beatline.BPMLabelText), (float)displayedTime));
                focusedTick = (0, PreviewType.none);
            }
            else
            {
                focusedTick = (displayedTick, PreviewType.BPM);
            }
        }
        else if (beatline.TSLabelVisible)
        {
            if (!SongTimelineManager.TimeSignatureEvents.ContainsKey(displayedTick))
            {
                SongTimelineManager.TimeSignatureEvents.Add(displayedTick, heldTS);
                focusedTick = (0, PreviewType.none);
            }
            else 
            {
                focusedTick = (displayedTick, PreviewType.TS);
            }
        }
        else return;
        TempoManager.UpdateBeatlines();
    }

    bool IsOverlayRaycasterHit()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        overlayUIRaycaster.Raycast(pointerData, results);

        if (results.Count > 0) return true; else return false;
    }
    
}
