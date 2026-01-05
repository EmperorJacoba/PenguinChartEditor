using System.Collections.Generic;
using UnityEngine;

public class FiveFretLane : SpawningLane<FiveFretNote>
{
    protected override bool cullAtStrikelineOnPlay => true;
    public FiveFretInstrument.LaneOrientation laneIdentifier;

    [SerializeField] FiveFretNotePooler lanePooler;
    FiveFretNotePreviewer previewer;

    // notes rely on this for their lane's sustain data
    public SustainData<FiveFretNoteData> sustainData = new();

    protected override IPooler<FiveFretNote> Pooler => (IPooler<FiveFretNote>)lanePooler;
    protected override IPreviewer Previewer
    {
        get
        {
            if (previewer == null)
            {
                previewer = transform.GetChild(0).gameObject.GetComponent<FiveFretNotePreviewer>();
            }
            return previewer;
        }
    }

    protected override List<int> GetEventsToDisplay()
    {
        var workingLane = parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier);
        var spawnStartTick = AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;
        return workingLane.GetRelevantTicksInRange(spawnStartTick, Waveform.endTick);
    }

    protected override int GetNextEvent(int tick)
    {
        return parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier).GetNextTickEventInLane(tick);
    }

    protected override int GetPreviousEvent(int tick)
    {
        var lane = parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier);
        return Mathf.Max(lane.GetPreviousTickEventInLane(tick), lane.GetFirstRelevantTick(tick));
    }
}