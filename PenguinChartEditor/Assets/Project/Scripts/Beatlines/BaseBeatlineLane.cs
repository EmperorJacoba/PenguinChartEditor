using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBeatlineLane<T> : Lane<T, BPMData> where T : IPoolable // BPMData is not acted upon here - any calls to it happen in base.Awake()
{
    protected override bool HasPreviewer() => false;
    protected override IPreviewer Previewer => null;

    protected override List<int> GetEventsToDisplay()
    {
        List<int> beatlineEvents = new();
        var firstTick = TimeSignature.GetNextBeatlineEvent(Waveform.startTick);
        var waveformEndBound = Mathf.Min(Waveform.endTick, SongTime.SongLengthTicks);

        for (int currentTick = firstTick;
            currentTick < waveformEndBound;
            currentTick = TimeSignature.GetNextBeatlineEventExclusive(currentTick)
            )
        {
            beatlineEvents.Add(currentTick);
        }
        return beatlineEvents;
    }
}
