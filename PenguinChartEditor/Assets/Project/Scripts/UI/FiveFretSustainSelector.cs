using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FiveFretSustainSelector : MonoBehaviour
{
    [SerializeField] TMP_InputField customSustainInput;
    [SerializeField] Button zeroButton;
    [SerializeField] Button maxButton;

    private void Awake()
    {
        customSustainInput.onValueChanged.AddListener(SetCustomInput);
        zeroButton.onClick.AddListener(SetZero);
        maxButton.onClick.AddListener(SetMax);
    }

    void SetZero()
    {
        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionSustain(0);
        customSustainInput.text = "";
    }
    void SetMax()
    {
        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionSustain(SongTime.SongLengthTicks);
        customSustainInput.text = "";
    }

    void SetCustomInput(string userInput)
    {
        if (!float.TryParse(userInput, out float beatsAsFloat)) return;

        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionSustain((int)Math.Ceiling(beatsAsFloat * Chart.Resolution));
    }

    public void ActivateCustomInput() => customSustainInput.ActivateInputField();
}