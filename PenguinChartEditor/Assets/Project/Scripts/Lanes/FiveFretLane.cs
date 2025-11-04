using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;

public class FiveFretLane : Lane<FiveFretNoteData>
{
    [SerializeField] public FiveFretInstrument.LaneOrientation laneIdentifier;
    [SerializeField] public FiveFretNotePooler lanePooler;
    [SerializeField] public FiveFretNotePreviewer previewer;

    public SustainData<FiveFretNoteData> sustainData = new();

    protected override void Awake()
    {
        base.Awake();
        Chart.ChartTabUpdated += UpdateEvents;
    }

    public void UpdateEvents()
    {
        var events = GetEventsToDisplay();

        int i;
        for (i = 0; i < events.Count; i++)
        {
            var note = lanePooler.GetObject(i);
            note.InitializeEvent(events[i], HighwayLength, laneIdentifier);
            note.lanePreviewer = previewer;
        }

        lanePooler.DeactivateUnused(i);
        previewer.UpdatePosition();
    }

    protected override List<int> GetEventsToDisplay()
    {
        var workingInstrument = (FiveFretInstrument)Chart.LoadedInstrument;

        return workingInstrument.Lanes.GetLane((int)laneIdentifier).
            Where(tick => (tick.Key <= Waveform.endTick) &&
            (tick.Key + tick.Value.Sustain >= Waveform.startTick)).
            Select(item => item.Key).ToList();
    }
}