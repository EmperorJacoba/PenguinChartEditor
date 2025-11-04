using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSLane : Lane<TSData>
{
    public static TSLane instance;

    [SerializeField] TSPooler pooler;

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
            var tsLabel = pooler.GetObject(i);
            tsLabel.InitializeEvent(events[i], HighwayLength);
        }

        pooler.DeactivateUnused(i);
        TSPreviewer.instance.UpdatePosition();
    }

    protected override List<int> GetEventsToDisplay() => 
        TimeSignature.Events.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();

}