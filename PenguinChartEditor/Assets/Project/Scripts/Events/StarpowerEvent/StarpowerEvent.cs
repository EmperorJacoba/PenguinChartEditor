using UnityEngine;

public class StarpowerEvent : Event<StarpowerEventData>, IPoolable
{
    protected override bool hasSustainTrail => true;
    public override int Lane => (int)laneID;
    public HeaderType laneID
    {
        get
        {
            return _li;
        }
        set
        {
            if (_li == value) return;

            _li = value;
            CacheDataReferences();
        }
    }
    private HeaderType _li = (HeaderType)(-1);

    private void CacheDataReferences()
    {
        _cachedSelectionRef = (SelectionSet<StarpowerEventData>)ParentInstrument.GetLaneSelection((int)laneID);
        _cachedDataRef = (LaneSet<StarpowerEventData>)ParentInstrument.GetLaneData((int)laneID);
    }

    public override SelectionSet<StarpowerEventData> Selection => _cachedSelectionRef;
    private SelectionSet<StarpowerEventData> _cachedSelectionRef;

    protected override LaneSet<StarpowerEventData> LaneData => _cachedDataRef;
    private LaneSet<StarpowerEventData> _cachedDataRef;

    [SerializeField] private StarpowerAnatomy notePieces;

    public override IInstrument ParentInstrument => Chart.StarpowerInstrument;

    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;

    public Coroutine destructionCoroutine { get; set; }

    public void InitializeProperties(ILane parentLane)
    {
        ParentLane = (StarpowerLane)parentLane;
        laneID = (HeaderType)ParentLane.laneID;
    }

    protected override void InitializeEvent()
    {
        UpdatePosition(Waveform.GetWaveformRatio(Tick), parentGameInstrument.GetLocalStarpowerXCoordinate());
        notePieces.UpdateSustainLength(Tick, representedData.Sustain);
    }

    protected override void InitializeEventAsPreviewer()
    {
        laneID = (HeaderType)ParentLane.laneID;
        
        UpdatePositionAsPreviewer();
        notePieces.UpdateSustainLength(Tick, representedData.Sustain);
        notePieces.ChangeColorToPreviewer();
    }

    private void UpdatePosition(double percentOfTrack, float xPosition, float yPosition = 0)
    {
        var trackProportion = (float)percentOfTrack * parentGameInstrument.HighwayLength;
        transform.localPosition = new Vector3(xPosition, yPosition, trackProportion);
    }

    private void UpdatePositionAsPreviewer() => 
        UpdatePosition(Waveform.GetWaveformRatio(Tick), parentGameInstrument.GetLocalStarpowerXCoordinate(), PREVIEWER_Y_OFFSET);
}