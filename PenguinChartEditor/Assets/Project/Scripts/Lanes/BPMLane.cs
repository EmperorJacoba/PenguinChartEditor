using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;

public class BPMLane : Lane<BPMData>
{
    public static BPMLane instance;
    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    public void UpdateEvents()
    {
        var events = GetEventsToDisplay();

        int i;
        for (i = 0; i < events.Count; i++)
        {
            var bpmLabel = BPMPooler.instance.GetObject(i);
            bpmLabel.InitializeEvent(events[i], HighwayLength);

        }

        BPMPooler.instance.DeactivateUnused(i);

        BPMPreviewer.instance.UpdatePosition();
    }

    protected override List<int> GetEventsToDisplay()
    {
        return Tempo.Events.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();
    }
}