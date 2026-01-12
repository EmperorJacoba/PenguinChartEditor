using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentTrackAdder : MonoBehaviour
{
    InstrumentAddBox parentBox;
    [SerializeField] InstrumentSpriteIcons iconReference;
    [SerializeField] Image instrumentIcon;
    [SerializeField] List<Button> DifficultyButtons;
    InstrumentType targetInstrument;

    public void Awake()
    {
        for (int i = 0; i < DifficultyButtons.Count; i++)
        {
            DifficultyType diff = (DifficultyType)i;
            DifficultyButtons[i].onClick.AddListener(() => SpawnInstrument(diff));
        }
    }

    public void SpawnInstrument(DifficultyType difficulty)
    {
        HeaderType instrumentID = InstrumentMetadata.GetHeader(targetInstrument, difficulty);
        if (parentBox.addDirectionIsRight)
        {
            InstrumentSpawningManager.instance.SpawnInstrumentOnRight(instrumentID);
        }
        else
        {
            InstrumentSpawningManager.instance.SpawnInstrumentOnLeft(instrumentID);
        }

        InstrumentAddBox.instance.gameObject.SetActive(false);
    }

    public void InitializeAs(InstrumentType instrument, InstrumentAddBox parentBox, List<DifficultyType> activeDifficulties)
    {
        this.parentBox = parentBox;
        targetInstrument = instrument;
        instrumentIcon.sprite = iconReference.GetInstrumentIcon((HeaderType)instrument);
        for (int i = 0; i < DifficultyButtons.Count; i++)
        {
            if (!activeDifficulties.Contains((DifficultyType)i))
            {
                DifficultyButtons[i].interactable = false;
            }
        }
    }
}