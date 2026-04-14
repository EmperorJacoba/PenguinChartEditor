using System.Collections.Generic;
using UnityEngine;

public class ChartSceneSpawner : MonoBehaviour
{
    [SerializeField] public InstrumentType representedInstrument;
    [SerializeField] private List<ChartTrackSpawningButton> buttons;
    
    public void SpawnTrack(DifficultyType difficulty)
    {
    }

    public void UpdateButton(DifficultyType difficulty)
    {
        buttons[(int)difficulty].UpdateButtonColors();
    }

    public HeaderType GetInstrumentID(DifficultyType difficultyType) =>
        InstrumentMetadata.GetHeader(representedInstrument, difficultyType);
}