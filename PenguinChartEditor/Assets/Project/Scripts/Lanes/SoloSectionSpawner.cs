using System.Linq;
using UnityEngine;

public class SoloSectionSpawner : MonoBehaviour
{
    [SerializeField] SoloSectionPooler pooler;

    private void Awake()
    {
        Chart.ChartTabUpdated += CheckForSoloDisplay;
    }

    void CheckForSoloDisplay()
    {
        var activeSoloSections = Chart.LoadedInstrument.SoloEvents.Where(@soloEvent => soloEvent.EndTick - Waveform.endTick > 0 || Waveform.startTick - soloEvent.StartTick > 0).ToList();
        if (activeSoloSections.Count == 0) return;

        int i;
        for (i = 0; i < activeSoloSections.Count; i++)
        {
            var soloSection = pooler.GetObject(i);
            soloSection.UpdateProperties(activeSoloSections[i].StartTick, activeSoloSections[i].EndTick);
        }
        pooler.DeactivateUnused(i);
    }
}
