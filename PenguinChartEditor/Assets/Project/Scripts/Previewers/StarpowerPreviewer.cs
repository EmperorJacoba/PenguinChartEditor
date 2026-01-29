using UnityEngine;

[RequireComponent(typeof(StarpowerEvent))]
public class StarpowerPreviewer : Previewer
{
    #region Event References

    private StarpowerEvent starpowerEvent => (StarpowerEvent)previewerEventReference;
    private StarpowerLane lane => (StarpowerLane)parentLane;
    private LaneSet<StarpowerEventData> actingStarpowerLane => Chart.StarpowerInstrument.GetLaneData(parentGameInstrument.representedInstrument.InstrumentID);

    #endregion
    
    protected override void UpdatePreviewer()
    {
        var hitPos = Chart.instance.SceneDetails.GetCursorHighwayPosition();

        if (!IsWithinRange(hitPos))
        {
            Hide();
            return;
        }

        var highwayProp = Chart.instance.SceneDetails.GetCursorHighwayProportion();

        Tick = SongTime.CalculateGridSnappedTick(highwayProp);

        StarpowerEventData previewData = new(
            false,
            AppliedSustain
            );
        
        starpowerEvent.InitializeEventAsPreviewer(lane, Tick, previewData);

        Show();
    }

    private bool IsWithinRange(Vector3 hitPosition)
    {
        var starpowerXCoordinate = starpowerEvent.parentGameInstrument.GetGlobalStarpowerXCoordinate();
        var halfLaneWidth = Chart.instance.SceneDetails.laneWidth / 2;
        if (hitPosition.x < (starpowerXCoordinate - halfLaneWidth) || hitPosition.x > (starpowerXCoordinate + halfLaneWidth) || hitPosition.y < 0)
        {
            return false;
        }
        return true;
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        int sustain =
            Chart.SyncTrackInstrument.ConvertTickDurationToSeconds(Tick, Tick + AppliedSustain) < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ?
            0 : AppliedSustain;

        actingStarpowerLane.Add(
            Tick,
            new StarpowerEventData(
                false,
                sustain
                )
            );
    }
}