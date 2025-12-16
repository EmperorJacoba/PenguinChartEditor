using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Each lane has its own set of notes and selections (EventData)
// Lanes are defined with type T.
// Notes are defined/calculated upon on a per-lane basis.

public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    public override LaneSet<FiveFretNoteData> LaneData => chartInstrument.Lanes.GetLane((int)laneIdentifier);
    public override SelectionSet<FiveFretNoteData> Selection => chartInstrument.Lanes.GetLaneSelection((int)laneIdentifier);

    [SerializeField] float defaultYPos = 0;
    [SerializeField] Transform sustain;
    [SerializeField] MeshRenderer sustainColor;
    [SerializeField] MeshRenderer noteColor;
    [SerializeField] NoteColors colors;
    [SerializeField] GameObject hopoTopper;
    [SerializeField] GameObject strumTopper;
    [SerializeField] GameObject tapTopper;
    [SerializeField] MeshRenderer headBorder;
    [SerializeField] bool previewer;

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
            if (colors != null)
            {
                noteColor.material = previewer ? colors.GetPreviewerMat(false) : colors.GetNoteMaterial((int)value, IsTap);
                sustainColor.material = previewer ? colors.GetPreviewerMat(false) : colors.GetNoteMaterial((int)value, IsTap);
            }
            _li = value;
        }
    }

    // starts as -1 so the redundancy check in laneIdentifier.set does not return true when setting lane to 0
    FiveFretInstrument.LaneOrientation _li = (FiveFretInstrument.LaneOrientation)(-1);

    public override IPreviewer EventPreviewer => lanePreviewer;
    public IPreviewer lanePreviewer; // define in pooler

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

            strumTopper.SetActive(!value);
            hopoTopper.SetActive(value);
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

            noteColor.material = previewer ? colors.GetPreviewerMat(value) : colors.GetNoteMaterial((int)laneIdentifier, value);

            // this script is also on opens
            // opens do not have head borders and thus borders will be null
            if (headBorder != null)
            {
                headBorder.material = colors.GetHeadColor(value);
            }

            strumTopper.SetActive(!value);
            tapTopper.SetActive(value);
            _isTap = value;
        }
    }
    bool _isTap = false;

    public override IInstrument ParentInstrument => chartInstrument;

    public void InitializeEvent(int tick, FiveFretInstrument.LaneOrientation lane, IPreviewer previewer)
    {
        _tick = tick;
        Visible = true;
        laneIdentifier = lane;
        lanePreviewer = previewer;

        InitializeNote();

        UpdatePosition(
            Waveform.GetWaveformRatio(tick),
            XCoordinate);

        UpdateSustain();

        var tickData = LaneData[tick];
        IsHopo = (tickData.Flag == FiveFretNoteData.FlagType.hopo);
        IsTap = (tickData.Flag == FiveFretNoteData.FlagType.tap);
    }
    public float XCoordinate => Chart.instance.SceneDetails.GetCenterXCoordinateFromLane((int)laneIdentifier);

    public override void RefreshLane() => parentLane.UpdateEvents();
    void InitializeNote()
    {
        if (SelectionOverlay != null) Selected = CheckForSelection();
    }

    public void UpdatePosition(double percentOfTrack, float xPosition)
    {
        var trackProportion = (float)percentOfTrack * Chart.instance.SceneDetails.HighwayLength;
        transform.position = new Vector3(xPosition, defaultYPos, trackProportion);
    }

    public FiveFretInstrument chartInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    public override void OnPointerDown(PointerEventData pointerEventData)
    {
        base.OnPointerDown(pointerEventData);
        
        if (pointerEventData.button == PointerEventData.InputButton.Right)
        {
            if (Input.GetKey(KeyCode.LeftShift) || !UserSettings.ExtSustains)
            {
                ParentInstrument.ShiftClickSelect(Tick, true);
                return;
            }
            Selection.Add(Tick);
        }
    }

    void UpdateSustain() => UpdateSustain(Tick, LaneData[Tick].Sustain);

    public void UpdateSustain(int tick, int sustainLength)
    {
        var sustainEndPointTicks = tick + sustainLength;

        var trackProportion = Waveform.GetWaveformRatio(sustainEndPointTicks);
        var trackPosition = trackProportion * Chart.instance.SceneDetails.HighwayLength;

        var noteProportion = Waveform.GetWaveformRatio(tick);
        var notePosition = noteProportion * Chart.instance.SceneDetails.HighwayLength;

        var localScaleZ = (float)(trackPosition - notePosition);

        // stop it from appearing past the end of the highway
        if (localScaleZ + transform.localPosition.z > Chart.instance.SceneDetails.HighwayLength) 
            localScaleZ = Chart.instance.SceneDetails.HighwayLength - transform.localPosition.z; 

        if (localScaleZ < 0) localScaleZ = 0; // box collider negative size issues??

        sustain.localScale = new Vector3(sustain.localScale.x, sustain.localScale.y, localScaleZ);
    }

    // used on sustain trail itself when click happens on trail
    // click on sustain trail + drag activates SustainSelection() within the previewer object
    public void ClampSustain(int tickLength) => chartInstrument.UpdateSustain(Tick, laneIdentifier, tickLength);
} 