using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretFlagSelector : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    [SerializeField] private bool naturalize;
    [SerializeField] private FiveFretNoteData.FlagType flag;

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
