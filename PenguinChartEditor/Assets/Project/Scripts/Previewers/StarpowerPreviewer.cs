using UnityEngine;

[RequireComponent(typeof(StarpowerEvent))]
public class StarpowerPreviewer : Previewer
{
    #region Event References

    private StarpowerEvent starpowerEvent => (StarpowerEvent)previewerEventReference;
    private StarpowerLane lane => (StarpowerLane)parentLane;
    private LaneSet<StarpowerEventData> actingStarpowerLane => Chart.StarpowerInstrument.GetLaneData(parentGameInstrument.representedInstrument.InstrumentID);

    #endregion
    
    protected override IEventData GetPreviewData()
    {
        return new StarpowerEventData(
            false,
            AppliedSustain
        );
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        if (Chart.LoadedInstrument != Chart.StarpowerInstrument) return false;
        
        var starpowerXCoordinate = starpowerEvent.parentGameInstrument.GetGlobalStarpowerXCoordinate();
        var halfLaneWidth = Chart.instance.SceneDetails.laneWidth / 2;
        if (hitPosition.x < (starpowerXCoordinate - halfLaneWidth) || hitPosition.x > (starpowerXCoordinate + halfLaneWidth) || hitPosition.y < 0)
        {
            return false;
        }
        return true;
    }
}