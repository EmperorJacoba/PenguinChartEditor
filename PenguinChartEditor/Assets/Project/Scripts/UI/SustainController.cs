using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class SustainController : MonoBehaviour
{
    [SerializeField] private TMP_InputField customSustainInput;
    [SerializeField] private Button zeroButton;
    [SerializeField] private Button maxButton;

    private void Awake()
    {
        customSustainInput.onEndEdit.AddListener(SetCustom);
        zeroButton.onClick.AddListener(SetZero);
        maxButton.onClick.AddListener(SetMax);
    }

    private void SetZero()
    {
        Previewer.defaultSustainTicks = 0;
        ClearInput();
    }

    private void SetMax()
    {
        Previewer.defaultSustainTicks = SongTime.SongLengthTicks;
        ClearInput();
    }

    public void ClearInput() => customSustainInput.text = "";

    private static void SetCustom(string value)
    {
        if (!float.TryParse(value, out var beatsAsFloat)) return;

        Previewer.defaultSustainTicks = (int)Math.Ceiling(beatsAsFloat * Chart.Resolution);
    }

    public void ActivateCustomInput() => customSustainInput.ActivateInputField();
}