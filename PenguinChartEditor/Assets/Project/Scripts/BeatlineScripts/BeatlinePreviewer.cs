using System;
using UnityEngine;

public class BeatlinePreviewer : MonoBehaviour
{
    [SerializeField] WaveformManager waveformManager;
    [SerializeField] Beatline beatline;
    [SerializeField] RectTransform screenReferenceRt;

    static InputMap inputMap;

    public bool editMode = true;

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position => UpdatePreviewPosition(position.ReadValue<Vector2>().y / screenReferenceRt.rect.height, position.ReadValue<Vector2>().x / screenReferenceRt.rect.width);
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

        waveformManager.GetCurrentDisplayedWaveformInfo(out var startTick, out var endTick, out var timeShown, out var startTime, out var endTime);

        var cursorTimestamp = (percentOfScreenVertical * timeShown) + startTime;
        var cursorTickTime = SongTimelineManager.ConvertSecondsToTickTime((float)cursorTimestamp); 

        var tickInterval = ChartMetadata.ChartResolution / ((float)DivisionChanger.CurrentDivision / 4);

        var divisionBasisTick = cursorTickTime - SongTimelineManager.FindLastBarline(cursorTickTime);
        var remainder = divisionBasisTick % tickInterval;

        int gridSnappedTick;
        if (remainder > (tickInterval / 2))
        {
            gridSnappedTick = (int)Math.Floor(cursorTickTime - remainder + tickInterval);
        }
        else
        {
            gridSnappedTick = (int)Math.Floor(cursorTickTime - remainder);
        }


        beatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(gridSnappedTick) - startTime)/timeShown);
        if (percentOfScreenHorizontal < 0.5f)
        {
            beatline.BPMLabelVisible = false;
            beatline.TSLabelVisible = true;
            beatline.TSLabelText = $"{SongTimelineManager.TimeSignatureEvents[SongTimelineManager.FindLastTSEventTick(gridSnappedTick)].Item1} / {SongTimelineManager.TimeSignatureEvents[SongTimelineManager.FindLastTSEventTick(gridSnappedTick)].Item2}";
        }
        else
        {
            beatline.BPMLabelVisible = true;
            beatline.TSLabelVisible = false;
            beatline.BPMLabelText = SongTimelineManager.TempoEvents[SongTimelineManager.FindLastTempoEventTick(gridSnappedTick)].Item1.ToString();
        }
        // get current cursor position
        // calculate the timestamp and following tick time
        // use current division to round that tick time to a current tick
        // place this beatline at that ticktime
    }
}
