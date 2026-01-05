using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBeatlineLane<T> : SpawningLane<T> where T : IPoolable // BPMData is not acted upon here - any calls to it happen in base.Awake()
{
    [HideInInspector] public override bool isReadOnly => true;
    protected override IPreviewer Previewer => null;

    protected override bool cullAtStrikelineOnPlay => false;

    protected override List<int> GetEventsToDisplay()
    {
        List<int> beatlineEvents = new();
        var firstTick = Chart.SyncTrackInstrument.GetNextBeatlineEvent(Waveform.startTick);
        var waveformEndBound = Mathf.Min(Waveform.endTick, SongTime.SongLengthTicks);

        for (int currentTick = firstTick;
            currentTick < waveformEndBound;
            currentTick = GetNextEvent(currentTick)
            )
        {
            beatlineEvents.Add(currentTick);
        }
        return beatlineEvents;
    }

    protected override int GetNextEvent(int tick)
    {
        return Chart.SyncTrackInstrument.GetNextBeatlineEventExclusive(tick);
    }

    protected override int GetPreviousEvent(int tick)
    {
        return Chart.SyncTrackInstrument.GetPreviousBeatlineEventExclusive(tick);
    }
}
