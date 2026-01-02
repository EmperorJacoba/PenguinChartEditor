using UnityEngine;

[RequireComponent(typeof(FiveFretNote))]
public class FiveFretNotePreviewer : Previewer
{
    FiveFretNote note; // use Previewer.Tick, not note.tick for any tick related actions
    FiveFretLane lane;
    float laneCenterPosition => note.xCoordinate;

    public static int defaultSustain = 0;

    int AppliedSustain => note.ParentFiveFretInstrument.CalculateSustainClamp(defaultSustain, Tick, lane.laneIdentifier);

    public static bool openNoteEditing = false;
    public bool OpenNoteEditing
    {
        get => openNoteEditing;
        set => openNoteEditing = value;
    }

    public enum NoteOption
    {
        natural,
        strum,
        hopo,
        tap
    }

    public static NoteOption currentPlacementMode = NoteOption.natural;

    // use as way to get dropdown to set mode
    public int PlacementMode
    {
        get => (int)currentPlacementMode;
        set => currentPlacementMode = (NoteOption)value;
    }

    protected override void UpdatePreviewer()
    {
        var hitPosition = Chart.instance.SceneDetails.GetCursorHighwayPosition();

        if (!IsWithinRange(hitPosition))
        {
            Hide();
            return;
        }

        var highwayProportion = Chart.instance.SceneDetails.GetCursorHighwayProportion();

        if (highwayProportion == 0)
        {
            Hide();
            return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);
        FiveFretNoteData.FlagType previewFlag;
        if (currentPlacementMode == NoteOption.natural)
        {
            previewFlag =
                Chart.GetActiveInstrument<FiveFretInstrument>().PreviewTickHopo(lane.laneIdentifier, Tick) ?
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
        if (lane == null) return false;

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

    public override void Hide()
    {
        if (note == null) return;
        if (note.Visible) note.Visible = false;
    }
    public override void Show()
    {
        if (note == null) return;
        if (!note.Visible) note.Visible = true;
    }

    protected override void Awake()
    {
        base.Awake();

        FiveFretNoteKeybindManager.UpdatePreviewer += UpdatePosition;
    }

    protected void Start()
    {
        lane = GetComponentInParent<FiveFretLane>();

        note = GetComponent<FiveFretNote>();
    }

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

    protected override void AddCurrentEventDataToLaneSet()
    {
        int sustain =
            Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(Tick + AppliedSustain) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(Tick) < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ?
            0 : AppliedSustain;

        Chart.GetActiveInstrument<FiveFretInstrument>().AddData(
            Tick,
            note.laneIdentifier,
            new FiveFretNoteData(
                sustain,
                MapPlacementModeToFlag(),
                currentPlacementMode == NoteOption.natural
                )
            );
    }
}
