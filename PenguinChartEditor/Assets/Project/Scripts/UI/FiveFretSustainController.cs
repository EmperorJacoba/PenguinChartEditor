using UnityEngine;
using System;
using TMPro;

public class FiveFretSustainController : MonoBehaviour
{
    [SerializeField] private TMP_InputField customSustainInput;

    private void Awake()
    {
        customSustainInput.onEndEdit.AddListener(SetCustom);
    }

    public void SetZero()
    {
        FiveFretNotePreviewer.defaultSustain = 0;
        ClearInput();
    }

    public void SetMax()
    {
        FiveFretNotePreviewer.defaultSustain = SongTime.SongLengthTicks;
        ClearInput();
    }

    public void ClearInput() => customSustainInput.text = "";

    public void SetCustom(string value)
    {
        if (!float.TryParse(value, out float beatsAsFloat)) return;

        FiveFretNotePreviewer.defaultSustain = (int)Math.Ceiling(beatsAsFloat * Chart.Resolution);
    }

    public void ActivateCustomInput() => customSustainInput.ActivateInputField();
}