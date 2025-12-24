using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretFlagSelector : MonoBehaviour
{
    Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    [SerializeField] bool naturalize;
    [SerializeField] FiveFretNoteData.FlagType flag;

    public void HandleClick()
    {
        if (naturalize)
        {
            Chart.GetActiveInstrument<FiveFretInstrument>().NaturalizeSelection();
            return;
        }
        Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionToFlag(flag);
    }
}
