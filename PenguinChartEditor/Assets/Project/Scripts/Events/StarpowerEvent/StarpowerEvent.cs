using UnityEngine;

public class StarpowerEvent : Event<StarpowerEventData>, IPoolable
{
    protected override bool HasSustainTrail => true;
    public override int Lane
    {
        get => (int)laneID;
        set
        {
            laneID = (HeaderType)value;
        }
    }

    private HeaderType laneID
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
    
    protected override void InitializeEvent()
    {
        notePieces.UpdateSustainLength(Tick, representedData.Sustain);
    }

    protected override void InitializeEventAsPreviewer()
    {
        notePieces.UpdateSustainLength(Tick, representedData.Sustain);
        notePieces.ChangeColorToPreviewer();
    }

    protected override void UpdatePosition()
    {
        var yPosition = IsPreviewEvent ? PREVIEWER_Y_OFFSET : 0;
        transform.localPosition = 
            new Vector3(
                parentGameInstrument.GetLocalStarpowerXCoordinate(), 
                yPosition, 
                GetDefaultZ()
            );
    }
}