using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentInclusionRow : MonoBehaviour
{
    [SerializeField] private InstrumentSpriteIcons iconReference;
    [SerializeField] private Image instrumentIcon;
    [SerializeField] private List<Button> difficultyButtons;
    private InstrumentType representedInstrument;

    [SerializeField] private Slider diffSlider;
    [SerializeField] private TMP_InputField diffManualInput;
    
    public void InitializeAs(InstrumentType instrument, List<DifficultyType> activeDifficulties)
    {
        representedInstrument = instrument;
        instrumentIcon.sprite = iconReference.GetInstrumentIcon((HeaderType)instrument);
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            if (!activeDifficulties.Contains((DifficultyType)i))
            {
                difficultyButtons[i].interactable = false;
            }
            else
            {
                difficultyButtons[i].image.color = activeColor;
            }
        }
        
        UpdateDiffDisplay();
    }

    private Color disabledColor = Color.white;
    private Color activeColor = Color.green;
    
    public void Awake()
    {
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            DifficultyType diff = (DifficultyType)i;
            difficultyButtons[i].onClick.AddListener(() => ToggleDifficultyInclusion(diff));
        }
        
        diffSlider.onValueChanged.AddListener(UpdateFromSlider);
        diffManualInput.onValueChanged.AddListener(UpdateFromManualInput);
    }

    private void ToggleDifficultyInclusion(DifficultyType difficultyType)
    {
        var targetButton = difficultyButtons[(int)difficultyType];
        targetButton.image.color = targetButton.image.color != activeColor ? activeColor : disabledColor;
    }

    private void UpdateDiffDisplay()
    {
        var diff = Chart.Metadata.GetDifficultyRating(representedInstrument);
        diffSlider.value = Mathf.Min(diff, 6);
        diffManualInput.text = diff.ToString();
    }

    private void UpdateFromSlider(float newValue)
    {
        Chart.Metadata.SetDifficultyRating(representedInstrument, (int)newValue);
        UpdateDiffDisplay();
    }

    private void UpdateFromManualInput(string newInput)
    {
        if (int.TryParse(newInput, out var diff))
        {
            Chart.Metadata.SetDifficultyRating(representedInstrument, diff);
        }
        UpdateDiffDisplay();
    }

    public Dictionary<DifficultyType, bool> GetActiveDifficulties()
    {
        var dict = new Dictionary<DifficultyType, bool>();

        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            dict[(DifficultyType)i] =
                difficultyButtons[i].interactable && difficultyButtons[i].image.color != disabledColor;
        }

        return dict;
    }
}