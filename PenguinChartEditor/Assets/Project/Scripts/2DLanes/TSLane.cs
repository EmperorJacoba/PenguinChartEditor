using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSLane : Lane<TSData>
{
    public static TSLane instance;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    public void UpdateEvents()
    {
        var eventsToDisplay = TimeSignature.Events.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();

        int warningCount = 0;
        int i;
        for (i = 0; i < eventsToDisplay.Count; i++)
        {
            var tsLabel = TSPooler.instance.ActivateObject(i, eventsToDisplay[i]);
            tsLabel.InitializeLabel();

            double percentOfScreen = (Tempo.ConvertTickTimeToSeconds(eventsToDisplay[i]) - Waveform.startTime) / Waveform.timeShown;

            tsLabel.UpdatePosition(percentOfScreen, boundaryReference2D.rect.height);

            if (!TimeSignature.IsEventValid(eventsToDisplay[i]))
            {
                var tsWarningAlert = WarningPooler.instance.ActivateObject(warningCount, eventsToDisplay[i]);

                tsWarningAlert.InitializeWarning(Warning.WarningType.invalidTimeSignature);
                tsWarningAlert.UpdatePosition(percentOfScreen, boundaryReference2D.rect.height);

                warningCount++;
            }
        }

        TSPooler.instance.DeactivateUnused(i);
        WarningPooler.instance.DeactivateUnused(warningCount);

        TSPreviewer.instance.UpdatePosition();
    }
}