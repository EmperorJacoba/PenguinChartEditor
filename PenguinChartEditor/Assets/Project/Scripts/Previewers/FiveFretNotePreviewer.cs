using UnityEngine;

[RequireComponent(typeof(FiveFretNote))]
public class FiveFretNotePreviewer : Previewer
{
    #region Event References

    FiveFretNote note => (FiveFretNote)previewerEventReference;
    FiveFretLane lane => (FiveFretLane)parentLane;
    FiveFretInstrument parentFiveFretInstrument => (FiveFretInstrument)lane.parentGameInstrument.representedInstrument;
    float laneCenterPosition => note.xCoordinate;

    #endregion

    #region Sustain Controlling

    public static int defaultSustain = 0;

    int AppliedSustain => note.ParentFiveFretInstrument.CalculateSustainClamp(defaultSustain, Tick, lane.laneIdentifier);

    #endregion

    #region NoteOption

    public static bool openNoteEditing = false;

    public enum NoteOption
    {
        natural,
        strum,
        hopo,
        tap
    }

    public static NoteOption currentPlacementMode = NoteOption.natural;

    FiveFretNoteData.FlagType MapPlacementModeToFlag()
    {
        return currentPlacementMode switch
        {
            NoteOption.hopo => FiveFretNoteData.FlagType.hopo,
            NoteOption.strum => FiveFretNoteData.FlagType.strum,
            NoteOption.tap => FiveFretNoteData.FlagType.tap,
            NoteOption.natural => FiveFretNoteData.FlagType.strum, // if dynamic, future algorithms will calculate the current type. Don't worry too much about it
            _ => throw new System.ArgumentException("If you got this error, you don't know how dropdowns work. Congratulations!"),
        };
    }

    #endregion

    protected override void UpdatePreviewer()
    {
        var hitPosition = lane.parentGameInstrument.GetCursorHighwayPosition();

        if (!IsWithinRange(hitPosition))
        {
            Hide();
            return;
        }

        var highwayProportion = lane.parentGameInstrument.GetCursorHighwayProportion();

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);

        FiveFretNoteData.FlagType previewFlag;
        if (currentPlacementMode == NoteOption.natural)
        {
            previewFlag =
                parentFiveFretInstrument.PreviewTickHopo(lane.laneIdentifier, Tick) ?
                FiveFretNoteData.FlagType.hopo : FiveFretNoteData.FlagType.strum;
        }
        else
        {
            previewFlag = MapPlacementModeToFlag();
        }

        FiveFretNoteData previewData = new(
            sustain: AppliedSustain,
            flag: previewFlag,
            defaultOrientation: currentPlacementMode == NoteOption.natural
            );

        note.InitializeEventAsPreviewer(lane, Tick, previewData);

        Show();
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
            if (hitPosition.x < (laneCenterPosition - 1) || hitPosition.x > (laneCenterPosition + 1) || hitPosition.y < 0) return false;
        }
        return true;
    }

    protected override void Awake()
    {
        base.Awake();

        FiveFretNoteKeybindManager.UpdatePreviewer += UpdatePosition;
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        int sustain =
            Chart.SyncTrackInstrument.ConvertTickDurationToSeconds(Tick, Tick + AppliedSustain) < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ?
            0 : AppliedSustain;

        parentFiveFretInstrument.AddData(
            Tick,
            note.laneID,
            new FiveFretNoteData(
                sustain,
                MapPlacementModeToFlag(),
                currentPlacementMode == NoteOption.natural
                )
            );
    }
}
