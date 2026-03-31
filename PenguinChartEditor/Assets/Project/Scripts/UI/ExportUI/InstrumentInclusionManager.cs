using System;
using UnityEngine;

public class InstrumentInclusionManager : MonoBehaviour
{
    [SerializeField] private GameObject instrumentInclusionRowPrefab;
    [SerializeField] private Transform scrollViewContent;

    private void Start()
    {
        foreach (var instrument in Chart.GetInstrumentDifficultyInformation())
        {
            var instrumentInclusionRow = Instantiate(instrumentInclusionRowPrefab, scrollViewContent)
                .GetComponent<InstrumentInclusionRow>();
            instrumentInclusionRow.InitializeAs(instrument.name, instrument.activeDifficulties);
        }
    }
}