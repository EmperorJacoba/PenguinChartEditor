using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FiveFretLane : Lane<FiveFretNoteData>
{
    [SerializeField] public FiveFretInstrument.LaneOrientation laneIdentifier;
    [SerializeField] FiveFretNotePooler lanePooler;
    [SerializeField] public FiveFretNotePreviewer previewer;
    [SerializeField] int laneCenterPosition;

    public SustainData<FiveFretNoteData> sustainData = new();

    protected override void Awake()
    {
        base.Awake();
        Chart.ChartTabUpdated += UpdateEvents;
    }

    public void UpdateEvents()
    {
        var workingInstrument = (FiveFretInstrument)Chart.LoadedInstrument;

        var eventsToDisplay = workingInstrument.Lanes[(int)laneIdentifier].
            Where(tick => (tick.Key <= Waveform.endTick) &&
            (tick.Key + tick.Value.Sustain >= Waveform.startTick)).
            Select(item => item.Key).ToList();

        int i = 0;
        for (i = 0; i < eventsToDisplay.Count; i++)
        {
            var note = lanePooler.ActivateObject(i, eventsToDisplay[i]);
            note.laneIdentifier = laneIdentifier;
            note.InitializeNote();

            note.UpdatePosition(
                (Tempo.ConvertTickTimeToSeconds(eventsToDisplay[i]) - Waveform.startTime) / Waveform.timeShown, 
                highway3D.localScale.z, 
                laneCenterPosition);
            note.UpdateSustain(highway3D.localScale.z);
        }

        lanePooler.DeactivateUnused(i);

        previewer.UpdatePosition();
    }
}