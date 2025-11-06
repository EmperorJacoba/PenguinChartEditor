using System.Collections.Generic;
using UnityEngine;

// This does not inherit from Lane<T> because it does not use event data
// Beatlines are specially generated based on time signature and tempo data
// Beatlines are not events themselves
public class BeatlineLane : MonoBehaviour
{
    [SerializeField] RectTransform boundaryReference;
    public static BeatlineLane instance;

    [SerializeField] BeatlinePooler pooler;

    void Awake()
    {
        instance = this;
        Chart.currentTab = Chart.TabType.TempoMap;
    }
    public void UpdateEvents()
    {
        var events = GetEventsToDisplay();

        int i;
        for (i = 0; i < events.Count; i++)
        {
            var beatline = pooler.GetObject(i);
            beatline.InitializeEvent(events[i], boundaryReference.rect.height);
        }
        pooler.DeactivateUnused(i);
    }

    protected List<int> GetEventsToDisplay()
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
