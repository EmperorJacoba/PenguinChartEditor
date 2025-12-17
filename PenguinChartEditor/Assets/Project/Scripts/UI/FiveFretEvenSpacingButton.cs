using UnityEngine.UI;
using UnityEngine;

public class FiveFretEvenSpacingButton : MonoBehaviour
{
    Button button;
    FiveFretInstrument ActiveInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SetEqualSpacing);
    }

    void SetEqualSpacing() => ActiveInstrument.SetEqualSpacing();
}