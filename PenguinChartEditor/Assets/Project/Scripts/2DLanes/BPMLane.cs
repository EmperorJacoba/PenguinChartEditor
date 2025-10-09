using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMLane : MonoBehaviour
{
    static RectTransform boundaryReference;
    public static BPMLane instance;
    void Awake()
    {
        boundaryReference = GameObject.Find("ScreenReference").GetComponent<RectTransform>();

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

            bpmLabel.UpdatePosition((Tempo.ConvertTickTimeToSeconds(eventsToDisplay[i]) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);
        }

        BPMPooler.instance.DeactivateUnused(i);

        BPMPreviewer.instance.UpdatePreviewPosition();
    }
    void Update()
    {
        UpdateEvents();
    }
}