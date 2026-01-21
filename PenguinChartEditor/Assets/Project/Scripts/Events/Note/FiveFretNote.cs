using UnityEngine;


public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    protected override bool hasSustainTrail => true;
    protected override LaneSet<FiveFretNoteData> LaneData => _cachedDataRef;
    private LaneSet<FiveFretNoteData> _cachedDataRef;
    public override SelectionSet<FiveFretNoteData> Selection => _cachedSelectionRef;
    private SelectionSet<FiveFretNoteData> _cachedSelectionRef;


    [SerializeField] private FiveFretAnatomy notePieces;

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

            notePieces.ChangeColor(value, IsTap, IsStarpower);

            _li = value;
            CacheDataReferences();
            CacheXCoordinate();
        }
    }

    // starts as -1 so the redundancy check in laneIdentifier.set does not return true when setting lane to 0
    private FiveFretInstrument.LaneOrientation _li = (FiveFretInstrument.LaneOrientation)(-1);

    private void CacheXCoordinate()
    {
        xCoordinate = parentGameInstrument.GetCenterXCoordinateFromLane((int)laneID);
    }

    private void CacheDataReferences()
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

    private bool _isHopo = false;

    public bool IsTap
    {
        get => _isTap;
        set
        {
            if (_isTap == value && !tapStarpowerColorRefreshNeeded) return;

            notePieces.ChangeTap(laneID, value, IsStarpower);
            _isTap = value;
            tapStarpowerColorRefreshNeeded = false;
        }
    }

    private bool _isTap = false;

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

    private bool _isDefault = true;

    public bool IsStarpower
    {
        get
        {
            return _isStarpower;
        }
        set
        {
            if (_isStarpower == value) return;

            _isStarpower = value;
            notePieces.ChangeColor(laneID, IsTap, IsStarpower);

            tapStarpowerColorRefreshNeeded = true;
        }
    }

    private bool _isStarpower = false;
    private bool tapStarpowerColorRefreshNeeded = false;

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
        Tick = tick;
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


    private void SetVisualProperties(FiveFretNoteData data)
    {
        IsStarpower = parentGameInstrument.IsTickStarpower(Tick);
        IsHopo = (data.Flag == FiveFretNoteData.FlagType.hopo);
        IsTap = (data.Flag == FiveFretNoteData.FlagType.tap);
        IsDefault = data.Default;
    }

    public void InitializeEventAsPreviewer(FiveFretLane parentLane, int previewTick, FiveFretNoteData previewData)
    {
        ParentLane = parentLane;
        laneID = (FiveFretInstrument.LaneOrientation)ParentLane.laneID;

        // do not use this with the previewer, use previewer's tick instead
        // but this is here for the functions below
        Tick = previewTick;

        UpdatePositionAsPreviewer();
        UpdateSustain(previewData);
        SetVisualProperties(previewData);
    }

    private bool CalculateHeadVisibility()
    {
        int headDespawnTick = AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;
        if (Tick <= headDespawnTick)
        {
            return false;
        }
        return true;
    }

    private void UpdatePositionAsPreviewer() => UpdatePosition(Waveform.GetWaveformRatio(Tick), xCoordinate, PREVIEWER_Y_OFFSET);
    private void UpdatePosition() => UpdatePosition(Waveform.GetWaveformRatio(Tick), xCoordinate);
    private void UpdatePosition(int tick) => UpdatePosition(Waveform.GetWaveformRatio(tick), xCoordinate);
    private void UpdatePosition(double percentOfTrack) => UpdatePosition(percentOfTrack, xCoordinate);

    private void UpdatePosition(double percentOfTrack, float xPosition, float yPosition = 0)
    {
        var trackProportion = (float)percentOfTrack * parentGameInstrument.HighwayLength;
        transform.localPosition = new Vector3(xPosition, yPosition, trackProportion);
    }

    private void UpdateSustain(bool headOnly)
    {
        // No math needed at all if sustain is 0
        if (representedData.Sustain == 0)
        {
            notePieces.SetSustainZero();
        }

        if (!headOnly && AudioManager.AudioPlaying)
        {
            notePieces.UpdateSustainLength(SongTime.SongPositionTicks, Tick + representedData.Sustain - SongTime.SongPositionTicks);
        }
        else
        {
            notePieces.UpdateSustainLength(Tick, representedData.Sustain);
        }
    }

    private void UpdateSustain(FiveFretNoteData data)
    {
        notePieces.UpdateSustainLength(Tick, data.Sustain);
    }
}