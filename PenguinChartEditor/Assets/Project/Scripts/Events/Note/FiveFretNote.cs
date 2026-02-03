using UnityEngine;

public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    protected override bool HasSustainTrail => true;

    #region Data References
    
    [SerializeField] private FiveFretAnatomy notePieces;

    protected override LaneSet<FiveFretNoteData> LaneData => _cachedDataRef;
    private LaneSet<FiveFretNoteData> _cachedDataRef;
    public override SelectionSet<FiveFretNoteData> Selection => _cachedSelectionRef;
    private SelectionSet<FiveFretNoteData> _cachedSelectionRef;
    
    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;
    public override IInstrument ParentInstrument => parentGameInstrument.representedInstrument;
    
    #endregion

    #region Lane Setup

    public override int Lane
    {
        get => (int)laneID;
        set
        {
            laneID = (FiveFretInstrument.LaneOrientation)value;
        }
    }
    
    public FiveFretInstrument.LaneOrientation laneID
    {
        get
        {
            return _li;
        }
        private set
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

    [HideInInspector] public float xCoordinate;
    
    private void CacheXCoordinate()
    {
        xCoordinate = parentGameInstrument.GetCenterXCoordinateFromLane((int)laneID);
    }

    private void CacheDataReferences()
    {
        _cachedDataRef = (LaneSet<FiveFretNoteData>)ParentInstrument.GetLaneData((int)laneID);
        _cachedSelectionRef = (SelectionSet<FiveFretNoteData>)ParentInstrument.GetLaneSelection((int)laneID);
    }


    #endregion

    #region Properties

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

    #endregion

    #region Init
    
    protected override void InitializeEvent()
    {
        bool isHeadVisible = CalculateHeadVisibility();

        notePieces.SetVisibility(isHeadVisible);

        UpdateSustain(isHeadVisible);
        SetVisualProperties(representedData);
    }
    
    protected override void InitializeEventAsPreviewer()
    {
        UpdateSustain(representedData);
        SetVisualProperties(representedData);
    }

    private void SetVisualProperties(FiveFretNoteData data)
    {
        IsStarpower = parentGameInstrument.IsTickStarpower(Tick);
        IsHopo = (data.Flag == FiveFretNoteData.FlagType.hopo);
        IsTap = (data.Flag == FiveFretNoteData.FlagType.tap);
        IsDefault = data.Default;
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
    
    #endregion

    #region Pos/Sustain

    protected override void UpdatePosition()
    {
        var yPosition = IsPreviewEvent ? PREVIEWER_Y_OFFSET : 0;
        transform.localPosition = 
            new Vector3(
                xCoordinate, 
                yPosition, 
                GetSpecifiedZ(AudioManager.AudioPlaying && !notePieces.IsNoteModelVisible ? SongTime.SongPositionTicks : Tick)
                );
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
    
    #endregion
}