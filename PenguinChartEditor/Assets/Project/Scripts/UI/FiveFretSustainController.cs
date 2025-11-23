using UnityEngine;
using System;
using TMPro;

public class FiveFretSustainController : MonoBehaviour
{
    [SerializeField] TMP_InputField customSustainInput;

    private void Awake()
    {
        customSustainInput.onEndEdit.AddListener(SetCustom);
    }

    public void SetZero()
    {
        FiveFretNotePreviewer.defaultSustain = 0;
        customSustainInput.text = "";
    }

    public void SetMax()
    {
        FiveFretNotePreviewer.defaultSustain = SongTime.SongLengthTicks;
        customSustainInput.text = "";
    }

    public void SetCustom(string value)
    {
        if (!float.TryParse(value, out float beatsAsFloat)) return;

        FiveFretNotePreviewer.defaultSustain = (int)Math.Floor(beatsAsFloat * Chart.Resolution);
    }
}