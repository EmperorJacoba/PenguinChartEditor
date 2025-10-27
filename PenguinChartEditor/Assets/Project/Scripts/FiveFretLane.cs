using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FiveFretLane : Lane<FiveFretNoteData>
{
    [SerializeField] public FiveFretInstrument.LaneOrientation laneIdentifier;
    [SerializeField] FiveFretNotePooler lanePooler;
    [SerializeField] public FiveFretNotePreviewer previewer;
    [SerializeField] int laneCenterPosition;

    protected override void Awake()
    {
        base.Awake();
        Chart.ChartTabUpdated += UpdateEvents;
    }

    public void UpdateEvents()
    {
        var workingInstrument = (FiveFretInstrument)Chart.LoadedInstrument;
        var eventsToDisplay = workingInstrument.Lanes[(int)laneIdentifier].Keys.
            Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();

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
        }

        lanePooler.DeactivateUnused(i);

        previewer.UpdatePosition();
    }
}