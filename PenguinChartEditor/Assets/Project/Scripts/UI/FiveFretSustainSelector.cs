using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class FiveFretSustainSelector : MonoBehaviour
{
    [SerializeField] TMP_InputField customSustainInput;
    [SerializeField] Button zeroButton;
    [SerializeField] Button maxButton;

    FiveFretInstrument ActiveInstrument => (FiveFretInstrument)Chart.LoadedInstrument;   
    private void Awake()
    {
        customSustainInput.onValueChanged.AddListener(SetCustomInput);
        zeroButton.onClick.AddListener(SetZero);
        maxButton.onClick.AddListener(SetMax);
    }

    void SetZero() => ActiveInstrument.SetSelectionSustain(0);
    void SetMax() => ActiveInstrument.SetSelectionSustain(SongTime.SongLengthTicks);

    void SetCustomInput(string userInput)
    {
        if (!float.TryParse(userInput, out float beatsAsFloat)) return;

        ActiveInstrument.SetSelectionSustain((int)Math.Ceiling(beatsAsFloat * Chart.Resolution));
    }
}