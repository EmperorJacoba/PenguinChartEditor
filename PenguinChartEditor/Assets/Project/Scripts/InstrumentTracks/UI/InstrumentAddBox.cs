using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pop-up box when clicking the green plus in multi-track (starpower) mode. Singleton.
/// </summary>
public class InstrumentAddBox : MonoBehaviour
{
    public static InstrumentAddBox instance;
    public bool addDirectionIsRight => _right;
    private bool _right = false;

    [SerializeField] private GameObject instrumentTrackAdderPrefab;
    [SerializeField] private Transform scrollViewContentTransform;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        instance = this;
        
        closeButton.onClick.AddListener(Deactivate);

        var instrumentData = Chart.GetInstrumentDifficultyInformation();

        foreach (var foundInstrument in instrumentData)
        {
            var trackAdder = Instantiate(instrumentTrackAdderPrefab, scrollViewContentTransform);
            var trackAdderComponent = trackAdder.GetComponent<InstrumentTrackAdder>();
            trackAdderComponent.InitializeAs(foundInstrument.name, this, foundInstrument.activeDifficulties);
        }

        gameObject.SetActive(false);
    }

    public void Activate(bool isRight)
    {
        gameObject.SetActive(true);
        _right = isRight;
    }

    void Deactivate()
    {
        gameObject.SetActive(false);
    }
}

public struct ActiveInstrument
{
    public InstrumentType name;
    public List<DifficultyType> activeDifficulties;

    public ActiveInstrument(InstrumentType instrument, DifficultyType firstDifficulty)
    {
        name = instrument;
        activeDifficulties = new List<DifficultyType>
        {
            firstDifficulty
        };
    }
}