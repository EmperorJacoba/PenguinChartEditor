using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FiveFretLane : Lane<FiveFretNote>
{
    public FiveFretInstrument.LaneOrientation laneIdentifier;

    [SerializeField] FiveFretNotePooler lanePooler;
    [SerializeField] FiveFretNotePreviewer previewer;

    // notes rely on this for their lane's sustain data
    public SustainData<FiveFretNoteData> sustainData = new();

    protected override IPooler<FiveFretNote> Pooler => (IPooler<FiveFretNote>)lanePooler;
    protected override IPreviewer Previewer => previewer;

    protected override void Awake()
    {
        base.Awake();
        Chart.ChartTabUpdated += UpdateEvents;
    }

    protected override List<int> GetEventsToDisplay()
    {
        var workingInstrument = (FiveFretInstrument)Chart.LoadedInstrument;

        return workingInstrument.Lanes.GetLane((int)laneIdentifier).
            Where(tick => (tick.Key <= Waveform.endTick) &&
            (tick.Key + tick.Value.Sustain >= Waveform.startTick)).
            Select(item => item.Key).ToList();
    }

    protected override void InitializeEvent(FiveFretNote @event, int tick) => @event.InitializeEvent(tick, laneIdentifier, previewer);
}