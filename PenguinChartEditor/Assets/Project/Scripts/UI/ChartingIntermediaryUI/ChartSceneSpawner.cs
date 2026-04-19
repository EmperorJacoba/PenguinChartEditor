using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChartSceneSpawner : MonoBehaviour
{
    [SerializeField] public InstrumentType representedInstrument;
    [SerializeField] private List<ChartTrackSpawningButton> buttons;
    
    public void SpawnTrack(DifficultyType difficulty)
    {
        var id = InstrumentMetadata.GetHeader(representedInstrument, difficulty);
        var sceneName = InstrumentMetadata.MatchInstrumentToSceneName(id);
        
        ChartSceneLoader.PrepareSceneLoad(id);

        SceneManager.UnloadSceneAsync("ChartingIntermediaryScene");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        SceneTabSwitcher.forceLoadedScene = sceneName;
    }

    public void UpdateButton(DifficultyType difficulty)
    {
        buttons[(int)difficulty].UpdateButtonColors();
    }

    public HeaderType GetInstrumentID(DifficultyType difficultyType) =>
        InstrumentMetadata.GetHeader(representedInstrument, difficultyType);
}