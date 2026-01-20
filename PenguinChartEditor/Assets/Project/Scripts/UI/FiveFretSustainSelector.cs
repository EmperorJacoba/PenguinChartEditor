using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FiveFretSustainSelector : MonoBehaviour
{
    [SerializeField] private TMP_InputField customSustainInput;
    [SerializeField] private Button zeroButton;
    [SerializeField] private Button maxButton;

    private void Awake()
    {
        customSustainInput.onValueChanged.AddListener(SetCustomInput);
        zeroButton.onClick.AddListener(SetZero);
        maxButton.onClick.AddListener(SetMax);
    }

    private void SetZero()
    {
        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionSustain(0);
        customSustainInput.text = "";
    }

    private void SetMax()
    {
        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionSustain(SongTime.SongLengthTicks);
        customSustainInput.text = "";
    }

    private void SetCustomInput(string userInput)
    {
        if (!float.TryParse(userInput, out float beatsAsFloat)) return;

        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionSustain((int)Math.Ceiling(beatsAsFloat * Chart.Resolution));
    }

    public void ActivateCustomInput() => customSustainInput.ActivateInputField();
}