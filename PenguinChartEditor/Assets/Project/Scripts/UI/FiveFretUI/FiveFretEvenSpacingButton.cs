using UnityEngine.UI;
using UnityEngine;

public class FiveFretEvenSpacingButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SetEqualSpacing);
    }

    private void SetEqualSpacing() => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
}