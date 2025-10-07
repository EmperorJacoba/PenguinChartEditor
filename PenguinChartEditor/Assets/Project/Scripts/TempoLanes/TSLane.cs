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

        int i = 0;
        for (i = 0; i < eventsToDisplay.Count; i++)
        {
            var tsLabel = TSPooler.instance.GetObject(i);
            tsLabel.Tick = eventsToDisplay[i];
            tsLabel.SetLabelActive();

            tsLabel.UpdatePosition((BPM.ConvertTickTimeToSeconds(eventsToDisplay[i]) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);
        }

        TSPooler.instance.DeactivateUnused(i);
    }

    void Update()
    {
        UpdateEvents();
    }
}