using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InstrumentInclusionManager : MonoBehaviour
{
    [SerializeField] private GameObject instrumentInclusionRowPrefab;
    [SerializeField] private Transform scrollViewContent;
    private List<InstrumentInclusionRow> activeInstrumentControlRows;

    private void Start()
    {
        activeInstrumentControlRows = new List<InstrumentInclusionRow>();
        foreach (var instrument in Chart.GetInstrumentDifficultyInformation())
        {
            var instrumentInclusionRow = Instantiate(instrumentInclusionRowPrefab, scrollViewContent)
                .GetComponent<InstrumentInclusionRow>();
            instrumentInclusionRow.InitializeAs(instrument.name, instrument.activeDifficulties);
            activeInstrumentControlRows.Add(instrumentInclusionRow);
        }
    }

    public Dictionary<InstrumentType, Dictionary<DifficultyType, bool>> GetActiveInstrumentTracks() =>
        activeInstrumentControlRows.ToDictionary(x => x.representedInstrument, x => x.GetActiveDifficulties());
}