using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(FiveFretNote))]
public class FiveFretNotePreviewer : Previewer
{
    [SerializeField] FiveFretNote note; // use Previewer.Tick, not note.tick for any tick related actions
    [SerializeField] Transform highway;
    [SerializeField] PhysicsRaycaster cameraHighwayRaycaster;
    [SerializeField] FiveFretLane lane;
    [SerializeField] int laneCenterPosition;

    public static int defaultSustain = 0;

    int AppliedSustain => note.chartInstrument.CalculateSustainClamp(defaultSustain, Tick, lane.laneIdentifier);

    public static bool openNoteEditing = false;
    public bool OpenNoteEditing
    {
        get => openNoteEditing;
        set => openNoteEditing = value;
    }

    public enum NoteOption
    {
        dynamic,
        strum,
        hopo,
        tap
    }

    public static NoteOption currentPlacementMode = NoteOption.dynamic;

    // use as way to get dropdown to set mode
    public int PlacementMode
    {
        get => (int)currentPlacementMode;
        set => currentPlacementMode = (NoteOption)value;
    }

    public int defaultSustainPlacement = 0;

    public override void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal)) return;

        var hitPosition = GetCursorHighwayPosition();
        var highwayProportion = GetCursorHighwayProportion();
        if (highwayProportion == 0)
        {
            Hide(); return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);

        if (note.LaneData.ContainsKey(Tick))
        {
            Hide(); return;
        }

        note.UpdatePosition(Waveform.GetWaveformRatio(Tick), highway.localScale.z, note.XCoordinate);
        note.UpdateSustain(Tick, AppliedSustain, highway.localScale.z);

        if (currentPlacementMode == NoteOption.dynamic)
        {
            note.IsHopo = note.chartInstrument.PreviewTickHopo(lane.laneIdentifier, Tick);
            note.IsTap = false;
        }
        else
        {
            note.IsHopo = currentPlacementMode == NoteOption.hopo;
            note.IsTap = currentPlacementMode == NoteOption.tap;
        }

        note.Visible = IsWithinRange(hitPosition);
    }

    bool IsWithinRange(Vector3 hitPosition)
    {
        // add code here to block open note from placing note if the cursor is above another note
        if (lane.laneIdentifier == FiveFretInstrument.LaneOrientation.open)
        {
            if (!openNoteEditing) return false;
        }
        else
        {
            if (openNoteEditing) return false;
            if (hitPosition.x < (laneCenterPosition - 1) || hitPosition.x > (laneCenterPosition + 1)) return false;
        }
        return true;
    }

    private Vector3 GetCursorHighwayPosition()
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(pointerData, results);

        if (results.Count == 0) return new Vector3(int.MinValue, int.MinValue, int.MinValue);

        return results[0].worldPosition;
    }

    /// <summary>
    /// Get the highway proportion but set the X value of the raycast to the center of the screen.
    /// </summary>
    /// <returns></returns>
    public override float GetCursorHighwayProportion()
    {
        PointerEventData modifiedPointerData = new(EventSystem.current)
        {
            position = new(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(modifiedPointerData, results);

        if (results.Count == 0) return 0;
        return results[0].worldPosition.z / highway.localScale.z;
    }
    public override void Hide()
    {
        if (note.Visible) note.Visible = false;
    }
    public override void Show()
    {
        if (!note.Visible) note.Visible = true;
    }

    protected override void Awake()
    {
        base.Awake();
        var fiveFretNote = GetComponent<FiveFretNote>();
        fiveFretNote.lanePreviewer = this;
        fiveFretNote.laneIdentifier = lane.laneIdentifier;

        FiveFretNoteKeybindManager.UpdatePreviewer += UpdatePosition;
    }

    FiveFretNoteData.FlagType MapPlacementModeToFlag()
    {
        return currentPlacementMode switch
        {
            NoteOption.hopo => FiveFretNoteData.FlagType.hopo,
            NoteOption.strum => FiveFretNoteData.FlagType.strum,
            NoteOption.tap => FiveFretNoteData.FlagType.tap,
            NoteOption.dynamic => FiveFretNoteData.FlagType.strum, // if dynamic, future algorithms will calculate the current type. Don't worry too much about it
            _ => throw new System.ArgumentException("If you got this error, you don't know how dropdowns work. Congratulations!"),
        };
    }

    public override void AddCurrentEventDataToLaneSet()
    {
        int sustain =
            Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(Tick + AppliedSustain) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(Tick) < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ?
            0 : AppliedSustain;

        note.CreateEvent(
            Tick,
            new FiveFretNoteData(
                sustain,
                MapPlacementModeToFlag(),
                currentPlacementMode == NoteOption.dynamic
                )
            );

        // this takes care of non-extended sustain calculations too
        note.chartInstrument.ClampSustainsBefore(Tick, lane.laneIdentifier);

        // make sure to update other events in the lane so that they are all the same type (hopo/strum/tap)

        note.chartInstrument.Lanes.ClearAllSelections();
    }
}
