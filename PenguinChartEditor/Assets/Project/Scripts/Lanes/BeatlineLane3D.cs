using System.Collections.Generic;
using UnityEngine;

// This does not inherit from Lane<T> because it does not use event data
// Beatlines are specially generated based on time signature and tempo data
// Beatlines are not events themselves
public class BeatlineLane3D : MonoBehaviour
{
    [SerializeField] Transform highway;
    public static BeatlineLane3D instance;

    [SerializeField] BeatlinePooler3D pooler;

    void Awake()
    {
        instance = this;
    }

    public void UpdateEvents()
    {
        var events = GetEventsToDisplay();

        int i;
        for (i = 0; i < events.Count; i++)
        {
            var beatline = pooler.GetObject(i);
            beatline.InitializeEvent(events[i], highway.localScale.z);
        }
        pooler.DeactivateUnused(i);
    }

    protected List<int> GetEventsToDisplay()
    {
        List<int> beatlineEvents = new();

        // not exclusive because we want to render the absolute next beatline event
        var firstTick = TimeSignature.GetNextBeatlineEvent(Waveform.startTick);
        var waveformEndBound = Mathf.Min(Waveform.endTick, SongTime.SongLengthTicks);

        for (int currentTick = firstTick;
            currentTick < waveformEndBound;
            // exclusive because regular would assign currentTick to itself
            currentTick = TimeSignature.GetNextBeatlineEventExclusive(currentTick)
            )
        {
            beatlineEvents.Add(currentTick);
        }
        return beatlineEvents;
    }
}
