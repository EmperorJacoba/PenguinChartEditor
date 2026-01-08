using UnityEngine;
using UnityEngine.EventSystems;

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

    public void InitializeProperties(ILane parentLane)
    {
        ParentLane = parentLane;
        laneID = (FiveFretInstrument.LaneOrientation)ParentLane.laneID;
    }

    public void InitializeEvent(int tick)
    {
        _tick = tick;
        representedData = LaneData[tick];

        bool isHeadVisible = CalculateHeadVisibility();

        notePieces.SetVisibility(isHeadVisible);

        if (!readOnly) CheckForSelection();

        UpdatePosition(
            tick: AudioManager.AudioPlaying && !isHeadVisible ? SongTime.SongPositionTicks : tick 
        );

        UpdateSustain(isHeadVisible);

        SetVisualProperties(representedData);
    }

    public void InitializeEventAsPreviewer(FiveFretLane parentLane, int previewTick, FiveFretNoteData previewData)
    {
        ParentLane = parentLane;
        laneID = (FiveFretInstrument.LaneOrientation)ParentLane.laneID;

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

    bool CalculateHeadVisibility()
    {
        int headDespawnTick = AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;
        if (Tick <= headDespawnTick)
        {
            return false;
        }
        return true;
    }

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
            Chart.InPlaceRefresh();
        }
    }

    void UpdateSustain(bool headOnly)
    {
        // No math needed at all if sustain is 0
        if (representedData.Sustain == 0)
        {
            notePieces.SetSustainZero();
        }

        if (!headOnly && AudioManager.AudioPlaying)
        {
            notePieces.UpdateSustainLength(SongTime.SongPositionTicks, Tick + representedData.Sustain - SongTime.SongPositionTicks, transform.localPosition.z);
        }
        else
        {
            notePieces.UpdateSustainLength(Tick, representedData.Sustain, transform.localPosition.z);
        }
    }

    void UpdateSustain(FiveFretNoteData data)
    {
        notePieces.UpdateSustainLength(_tick, data.Sustain, transform.localPosition.z);
    }

    // used on sustain trail itself when click happens on trail
    // click on sustain trail + drag activates SustainSelection() within the previewer object
    public void ClampSustain(int tickLength) => ParentFiveFretInstrument.UpdateSustain(Tick, laneID, tickLength);

    /*
    void IPoolable.UpdatePosition()
    {
        bool isHeadVisible = CalculateHeadVisibility();
        notePieces.SetVisibility(isHeadVisible);

        UpdatePosition(
            tick: AudioManager.AudioPlaying && !isHeadVisible ? SongTime.SongPositionTicks : Tick
            );
        UpdateSustain(isHeadVisible);
    } */
}