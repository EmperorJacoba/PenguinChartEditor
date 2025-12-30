using UnityEngine.UI;
using UnityEngine;

namespace Penguin.FiveFretUI
{
    public class FiveFretEvenSpacingButton : MonoBehaviour
    {
        Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(SetEqualSpacing);
        }

        void SetEqualSpacing() => Chart.GetActiveInstrument<FiveFretInstrument>().SetEqualSpacing();
    }
}