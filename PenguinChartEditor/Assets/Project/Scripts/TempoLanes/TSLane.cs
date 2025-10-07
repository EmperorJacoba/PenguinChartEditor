using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSLane : MonoBehaviour
{
    static RectTransform boundaryReference;

    void Awake()
    {
        boundaryReference = GameObject.Find("ScreenReference").GetComponent<RectTransform>();
    }
    public void UpdateEvents()
    {
        var eventsToDisplay = TimeSignature.EventData.Events.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();

        int warningCount = 0;
        int i = 0;
        for (i = 0; i < eventsToDisplay.Count; i++)
        {
            var tsLabel = TSPooler.instance.GetObject(i);
            tsLabel.Tick = eventsToDisplay[i];
            tsLabel.SetLabelActive();

            double percentOfScreen = (BPM.ConvertTickTimeToSeconds(eventsToDisplay[i]) - Waveform.startTime) / Waveform.timeShown;

            tsLabel.UpdatePosition(percentOfScreen, boundaryReference.rect.height);

            if (!TimeSignature.IsEventValid(eventsToDisplay[i]))
            {
                var tsWarningAlert = WarningPooler.instance.GetObject(warningCount);
                tsWarningAlert.InitializeWarning(Warning.WarningType.invalidTimeSignature);
                tsWarningAlert.UpdatePosition(percentOfScreen, boundaryReference.rect.height);
                warningCount++;
            }
        }

        TSPooler.instance.DeactivateUnused(i);
        WarningPooler.instance.DeactivateUnused(i);
    }

    void Update()
    {
        UpdateEvents();
    }
}