using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretFlagSelector : MonoBehaviour
{
    Button button;
    FiveFretInstrument ActiveInstrument => (FiveFretInstrument)Chart.LoadedInstrument;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    [SerializeField] bool naturalize;
    [SerializeField] FiveFretNoteData.FlagType flag;

    void HandleClick()
    {
        if (naturalize)
        {
            ActiveInstrument.NaturalizeSelection();
            return;
        }
        ActiveInstrument.SetSelectionToFlag(flag);
    }
}
