using System.Collections.Generic;
using UnityEngine;

public class FiveFretLane : SpawningLane<FiveFretNote>
{
    [SerializeField] FiveFretNotePooler lanePooler;
    protected override IPooler<FiveFretNote> Pooler => lanePooler;
    protected override bool cullAtStrikelineOnPlay => true;
    public FiveFretInstrument.LaneOrientation laneIdentifier;

    protected override List<int> GetEventsToDisplay()
    {
        var workingLane = parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier);
        var spawnStartTick = AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;
        return workingLane.GetRelevantTicksInRange(spawnStartTick, Waveform.endTick);
    }

    protected override int GetNextEventUpdate(int tick)
    {
        return parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier).GetNextTickEventInLane(tick);
    }

    protected override int GetPreviousEventUpdate(int tick)
    {
        var lane = parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier);
        return Mathf.Max(lane.GetPreviousTickEventInLane(tick), lane.GetFirstRelevantTick(tick));
    }
}