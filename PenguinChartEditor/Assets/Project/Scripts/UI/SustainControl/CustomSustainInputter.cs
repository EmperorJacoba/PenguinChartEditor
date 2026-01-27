using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomSustainInputter : MonoBehaviour
{
    [SerializeField] private BeatsOrBarsSelector beatsOrBarsSelector;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private List<Button> disableOnButtonPressCandidates;
    
    [SerializeField] private bool isSelection;

    private void Awake()
    {
        inputField.onValueChanged.AddListener(ChangeRequiredSustainValue);

        foreach (var button in disableOnButtonPressCandidates)
        {
            button.onClick.AddListener(ClearInput);
        }
    }

    public void ClearInput()
    {
        inputField.text = "";
    }

    private void ChangeRequiredSustainValue(string targetInput)
    {
        float appliedDuration;
        if (!float.TryParse(targetInput, out appliedDuration))
        {
            appliedDuration = 0;
            return;
        }
        
        if (isSelection) ApplySelectionSustain(appliedDuration);
        else ApplyPreviewerSustain(appliedDuration);
        
        Chart.InPlaceRefresh();
    }
    
    private void ApplySelectionSustain(float appliedDuration)
    {
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
    }

    private void ApplyPreviewerSustain(float appliedDuration)
    {
        switch (beatsOrBarsSelector.GetUnitSelectionMode())
        {
            case BeatsOrBarsSelector.JumpLengthOptions.Bars:
                Previewer.SetDefaultSustainLength(false, appliedDuration);
                break;
            case BeatsOrBarsSelector.JumpLengthOptions.Beats:
                Previewer.SetDefaultSustainLength(true, (int)(appliedDuration * Chart.Resolution));
                break;
            default:
                throw new ArgumentException("Tried to handle invalid unit of selection. Only beats/bars supported.");
        }
    }
    
    public void ActivateCustomInput() => inputField.ActivateInputField();
}