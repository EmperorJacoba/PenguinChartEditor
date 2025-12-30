using UnityEngine;
using UnityEngine.EventSystems;

// Each lane has its own set of notes and selections (EventData)
// Lanes are defined with type T.
// Notes are defined/calculated upon on a per-lane basis.

public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    public override LaneSet<FiveFretNoteData> LaneData => chartInstrument.GetLaneData(laneIdentifier);
    public override SelectionSet<FiveFretNoteData> Selection => chartInstrument.GetLaneSelection(laneIdentifier);

    private const float PREVIEWER_Y_OFFSET = 0.00001f;

    [SerializeField] FiveFretAnatomy notePieces;

    public Coroutine destructionCoroutine { get; set; }

    public override int Lane => (int)laneIdentifier;
    public FiveFretInstrument.LaneOrientation laneIdentifier
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
            CacheXCoordinate();
        }
    }

    void CacheXCoordinate()
    {
        xCoordinate = Chart.instance.SceneDetails.GetCenterXCoordinateFromLane((int)laneIdentifier);
    }
    public float xCoordinate;


    // starts as -1 so the redundancy check in laneIdentifier.set does not return true when setting lane to 0
    FiveFretInstrument.LaneOrientation _li = (FiveFretInstrument.LaneOrientation)(-1);

    public override IPreviewer EventPreviewer => LanePreviewer;
    public IPreviewer LanePreviewer
    {
        get => _prevobj;
        set
        {
            if (_prevobj == value) return;
            _prevobj = value;
        }
    } // define in pooler
    IPreviewer _prevobj;

    FiveFretLane parentLane
    {
        get
        {
            if (_lane == null)
            {
                _lane = GetComponentInParent<FiveFretLane>();
            }
            return _lane;
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

            notePieces.ChangeTap(laneIdentifier, value);
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

    public override IInstrument ParentInstrument => chartInstrument;

    public FiveFretNoteData representedData;

    public void InitializeEvent(int tick, FiveFretInstrument.LaneOrientation lane, IPreviewer previewer, bool asSustainOnly)
    {
        _tick = tick;
        laneIdentifier = lane;
        LanePreviewer = previewer;
        representedData = LaneData[tick];

        InitializeNote(!asSustainOnly);

        UpdatePosition(
            tick: asSustainOnly ? SongTime.SongPositionTicks : tick
        );

        UpdateSustain(asSustainOnly);

        SetVisualProperties(representedData);
    }

    public void InitializeEventAsPreviewer(int previewTick, FiveFretNoteData previewData)
    {
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

    public override void RefreshLane() => parentLane.UpdateEvents();
    void InitializeNote(bool includeNoteHead)
    {
        if (!includeNoteHead)
        {
            notePieces.SetVisibility(false);
        }
        else
        {
            notePieces.SetVisibility(true);
            CheckForSelection();
        }
    }

    public void UpdatePositionAsPreviewer() => UpdatePosition(Waveform.GetWaveformRatio(_tick), xCoordinate, PREVIEWER_Y_OFFSET);
    public void UpdatePosition() => UpdatePosition(Waveform.GetWaveformRatio(_tick), xCoordinate);
    public void UpdatePosition(int tick) => UpdatePosition(Waveform.GetWaveformRatio(tick), xCoordinate);
    public void UpdatePosition(double percentOfTrack) => UpdatePosition(percentOfTrack, xCoordinate);
    public void UpdatePosition(double percentOfTrack, float xPosition, float yPosition = 0)
    {
        var trackProportion = (float)percentOfTrack * Chart.instance.SceneDetails.HighwayLength;
        transform.position = new Vector3(xPosition, yPosition, trackProportion);
    }

    public FiveFretInstrument chartInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);

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
    public void ClampSustain(int tickLength) => chartInstrument.UpdateSustain(Tick, laneIdentifier, tickLength);
}