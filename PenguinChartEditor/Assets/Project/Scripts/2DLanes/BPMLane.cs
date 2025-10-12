using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        var eventsToDisplay = Tempo.Events.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();

        int i = 0;
        for (i = 0; i < eventsToDisplay.Count; i++)
        {
            var bpmLabel = BPMPooler.instance.GetObject(i);
            bpmLabel.Tick = eventsToDisplay[i];
            bpmLabel.SetLabelActive();

            // w/o this the input field will stay on if you delete it while editing
            // leading to jank where the input field for the next event is visible
            // but was never edited
            if (BPMLabel.justDeleted) bpmLabel.DeactivateManualInput();

            bpmLabel.UpdatePosition((Tempo.ConvertTickTimeToSeconds(eventsToDisplay[i]) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);
        }

        BPMPooler.instance.DeactivateUnused(i);

        BPMPreviewer.instance.UpdatePosition();
        BPMLabel.justDeleted = false;
    }
}