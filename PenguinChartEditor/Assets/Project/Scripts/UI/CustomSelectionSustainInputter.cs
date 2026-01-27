using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomSelectionSustainInputter : MonoBehaviour
{
    [SerializeField] private BeatsOrBarsSelector beatsOrBarsSelector;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private List<Button> disableOnButtonPressCandidates;

    private void Awake()
    {
        inputField.onValueChanged.AddListener(ApplySelectionSustain);

        foreach (var button in disableOnButtonPressCandidates)
        {
            button.onClick.AddListener(() => inputField.text = "");
        }
    }

    private void ApplySelectionSustain(string targetInput)
    {
        float appliedDuration;
        if (!float.TryParse(targetInput, out appliedDuration))
        {
            appliedDuration = 0;
            return;
        }

        switch (beatsOrBarsSelector.GetUnitSelectionMode())
        {
            case BeatsOrBarsSelector.JumpLengthOptions.Bars:
                Chart.LoadedSustainableInstrument.SetSelectionSustain(appliedDuration);
                break;
            case BeatsOrBarsSelector.JumpLengthOptions.Beats:
                Chart.LoadedSustainableInstrument.SetSelectionSustain((int)(appliedDuration * Chart.Resolution));
                break;
            default:
                throw new ArgumentException("Tried to handle invalid unit of selection. Only beats/bars supported.");
        }
        
        Chart.InPlaceRefresh();
    }
}