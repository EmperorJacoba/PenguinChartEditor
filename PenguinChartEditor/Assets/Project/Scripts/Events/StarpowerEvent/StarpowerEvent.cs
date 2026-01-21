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

    public void InitializeEvent(int tick)
    {
        Tick = tick;
        representedData = LaneData[tick];

        if (!readOnly) CheckForSelection();

        UpdatePosition(Waveform.GetWaveformRatio(tick), parentGameInstrument.GetLocalStarpowerXCoordinate());
        notePieces.UpdateSustainLength(tick, representedData.Sustain);
    }

    public void InitializeEventAsPreviewer(StarpowerLane parentLane, int previewTick, StarpowerEventData previewData)
    {
        ParentLane = parentLane;
        laneID = (HeaderType)ParentLane.laneID;

        Tick = previewTick;

        UpdatePositionAsPreviewer();
        notePieces.UpdateSustainLength(previewTick, previewData.Sustain);
        notePieces.ChangeColorToPreviewer();
    }

    private void UpdatePosition(double percentOfTrack, float xPosition, float yPosition = 0)
    {
        var trackProportion = (float)percentOfTrack * parentGameInstrument.HighwayLength;
        transform.localPosition = new(xPosition, yPosition, trackProportion);
    }

    private void UpdatePositionAsPreviewer() => 
        UpdatePosition(Waveform.GetWaveformRatio(Tick), parentGameInstrument.GetLocalStarpowerXCoordinate(), PREVIEWER_Y_OFFSET);
}