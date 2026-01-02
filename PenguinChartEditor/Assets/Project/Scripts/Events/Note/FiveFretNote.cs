using UnityEngine;
using UnityEngine.EventSystems;

// Each lane has its own set of notes and selections (EventData)
// Lanes are defined with type T.
// Notes are defined/calculated upon on a per-lane basis.

public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    public override LaneSet<FiveFretNoteData> LaneData => _cachedDataRef;
    private LaneSet<FiveFretNoteData> _cachedDataRef;
    public override SelectionSet<FiveFretNoteData> Selection => _cachedSelectionRef;
    private SelectionSet<FiveFretNoteData> _cachedSelectionRef;

    private const float PREVIEWER_Y_OFFSET = 0.00001f;

    [SerializeField] FiveFretAnatomy notePieces;

    public Coroutine destructionCoroutine { get; set; }

    public override int Lane => (int)laneID;
    public FiveFretInstrument.LaneOrientation laneID
    {
        get
        {
            return _li;
        }
        set
        {
            if (_li == value) return;

            notePieces.ChangeColor(value, IsTap);

            _li = value;
            CacheDataReferences();
            CacheXCoordinate();
        }
    }

    // starts as -1 so the redundancy check in laneIdentifier.set does not return true when setting lane to 0
    FiveFretInstrument.LaneOrientation _li = (FiveFretInstrument.LaneOrientation)(-1);

    void CacheXCoordinate()
    {
        xCoordinate = parentGameInstrument.GetCenterXCoordinateFromLane((int)laneID);
    }

    void CacheDataReferences()
    {
        _cachedDataRef = (LaneSet<FiveFretNoteData>)ParentInstrument.GetLaneData((int)laneID);
        _cachedSelectionRef = (SelectionSet<FiveFretNoteData>)ParentInstrument.GetLaneSelection((int)laneID);
    }

    [HideInInspector] public float xCoordinate;

    FiveFretLane ParentLane
    {
        get
        {
            if (_lane == null)
            {
                _lane = GetComponentInParent<FiveFretLane>();
            }
            return _lane;
        }
        set
        {
            if (_lane == value) return;
            _lane = value;
        }
    }
    FiveFretLane _lane;

    public bool IsHopo
    {
        get => _isHopo;
        set
        {
            if (_isHopo == value) return;

            notePieces.ChangeHopo(value);
            _isHopo = value;
        }
    }
    bool _isHopo = false;

    public bool IsTap
    {
        get => _isTap;
        set
        {
            if (_isTap == value) return;

            notePieces.ChangeTap(laneID, value);
            _isTap = value;
        }
    }
    bool _isTap = false;

    public bool IsDefault
    {
        get => _isDefault;
        set
        {
            if (_isDefault == value) return;

            notePieces.ChangeDefault(value);
            _isDefault = value;
        }
    }
    bool _isDefault = true;

    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;
    public override IInstrument ParentInstrument => parentGameInstrument.representedInstrument;
    public FiveFretInstrument ParentFiveFretInstrument => (FiveFretInstrument)ParentInstrument;

    public FiveFretNoteData representedData;

    public void InitializeEvent(FiveFretLane parentLane, int tick, bool asSustainOnly)
    {
        ParentLane = parentLane;
        laneID = ParentLane.laneIdentifier;

        _tick = tick;
        representedData = LaneData[tick];

        notePieces.SetVisibility(!asSustainOnly);
        if (!readOnly) CheckForSelection();

        UpdatePosition(
            tick: asSustainOnly ? SongTime.SongPositionTicks : tick
        );

        UpdateSustain(asSustainOnly);

        SetVisualProperties(representedData);
    }

    public void InitializeEventAsPreviewer(FiveFretLane parentLane, int previewTick, FiveFretNoteData previewData)
    {
        ParentLane = parentLane;
        laneID = ParentLane.laneIdentifier;

        // do not use this with the previewer, use previewer's tick instead
        // but this is here just in case & for the functions below
        _tick = previewTick;

        UpdatePositionAsPreviewer();
        UpdateSustain(previewData);
        SetVisualProperties(previewData);
    }

    void SetVisualProperties(FiveFretNoteData data)
    {
        IsHopo = (data.Flag == FiveFretNoteData.FlagType.hopo);
        IsTap = (data.Flag == FiveFretNoteData.FlagType.tap);
        IsDefault = data.Default;
    }

    public override void RefreshLane() => ParentLane.UpdateEvents();

    public void UpdatePositionAsPreviewer() => UpdatePosition(Waveform.GetWaveformRatio(_tick), xCoordinate, PREVIEWER_Y_OFFSET);
    public void UpdatePosition() => UpdatePosition(Waveform.GetWaveformRatio(_tick), xCoordinate);
    public void UpdatePosition(int tick) => UpdatePosition(Waveform.GetWaveformRatio(tick), xCoordinate);
    public void UpdatePosition(double percentOfTrack) => UpdatePosition(percentOfTrack, xCoordinate);
    public void UpdatePosition(double percentOfTrack, float xPosition, float yPosition = 0)
    {
        var trackProportion = (float)percentOfTrack * parentGameInstrument.HighwayLength;
        transform.localPosition = new Vector3(xPosition, yPosition, trackProportion);
    }


    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);

        if (readOnly) return;

        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            if (Input.GetKey(KeyCode.LeftShift) || !UserSettings.ExtSustains)
            {
                ParentInstrument.ShiftClickSelect(Tick);
                return;
            }
            Selection.Add(Tick);
            Chart.Refresh();
        }
    }

    void UpdateSustain(bool sustainOnly)
    {
        if (sustainOnly)
        {
            notePieces.UpdateSustainLength(SongTime.SongPositionTicks, Tick + LaneData[Tick].Sustain - SongTime.SongPositionTicks, transform.localPosition.z);
        }
        else
        {
            notePieces.UpdateSustainLength(Tick, LaneData[Tick].Sustain, transform.localPosition.z);
        }
    }

    void UpdateSustain(FiveFretNoteData data)
    {
        notePieces.UpdateSustainLength(_tick, data.Sustain, transform.localPosition.z);
    }

    // used on sustain trail itself when click happens on trail
    // click on sustain trail + drag activates SustainSelection() within the previewer object
    public void ClampSustain(int tickLength) => ParentFiveFretInstrument.UpdateSustain(Tick, laneID, tickLength);
}