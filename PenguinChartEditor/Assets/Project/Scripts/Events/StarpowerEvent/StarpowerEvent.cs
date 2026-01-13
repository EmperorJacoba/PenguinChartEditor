using UnityEngine;

public class StarpowerEvent : Event<StarpowerEventData>, IPoolable
{
    public override bool hasSustainTrail => true;
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

    void CacheDataReferences()
    {
        _cachedSelectionRef = (SelectionSet<StarpowerEventData>)ParentInstrument.GetLaneSelection((int)laneID);
        _cachedDataRef = (LaneSet<StarpowerEventData>)ParentInstrument.GetLaneData((int)laneID);
    }

    public override SelectionSet<StarpowerEventData> Selection => _cachedSelectionRef;
    private SelectionSet<StarpowerEventData> _cachedSelectionRef;

    public override LaneSet<StarpowerEventData> LaneData => _cachedDataRef;
    private LaneSet<StarpowerEventData> _cachedDataRef;

    [SerializeField] StarpowerAnatomy notePieces;

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
        _tick = tick;
        representedData = LaneData[tick];

        if (!readOnly) CheckForSelection();

        UpdatePosition(Waveform.GetWaveformRatio(tick), parentGameInstrument.GetLocalStarpowerXCoordinate());
        notePieces.UpdateSustainLength(tick, representedData.Sustain);
    }

    public void InitializeEventAsPreviewer(StarpowerLane parentLane, int previewTick, StarpowerEventData previewData)
    {
        ParentLane = parentLane;
        laneID = (HeaderType)ParentLane.laneID;

        _tick = previewTick;

        UpdatePositionAsPreviewer();
        notePieces.UpdateSustainLength(previewTick, previewData.Sustain);
        notePieces.ChangeColorToPreviewer();
    }

    void UpdatePosition(double percentOfTrack, float xPosition, float yPosition = 0)
    {
        var trackProportion = (float)percentOfTrack * parentGameInstrument.HighwayLength;
        transform.localPosition = new(xPosition, yPosition, trackProportion);
    }

    void UpdatePositionAsPreviewer() => 
        UpdatePosition(Waveform.GetWaveformRatio(_tick), parentGameInstrument.GetLocalStarpowerXCoordinate(), PREVIEWER_Y_OFFSET);
}