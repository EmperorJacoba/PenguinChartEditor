using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBeatlineLane<T> : Lane<T> where T : IPoolable // BPMData is not acted upon here - any calls to it happen in base.Awake()
{
    protected override bool HasPreviewer() => false;
    protected override IPreviewer Previewer => null;

    protected override List<int> GetEventsToDisplay()
    {
        List<int> beatlineEvents = new();
        var firstTick = Chart.SyncTrackInstrument.GetNextBeatlineEvent(Waveform.startTick);
        var waveformEndBound = Mathf.Min(Waveform.endTick, SongTime.SongLengthTicks);

        Chart.Log($"{firstTick}, {waveformEndBound}");

        for (int currentTick = firstTick;
            currentTick < waveformEndBound;
            currentTick = Chart.SyncTrackInstrument.GetNextBeatlineEventExclusive(currentTick)
            )
        {
            Chart.Log($"{currentTick}");
            beatlineEvents.Add(currentTick);
        }
        return beatlineEvents;
    }
}
