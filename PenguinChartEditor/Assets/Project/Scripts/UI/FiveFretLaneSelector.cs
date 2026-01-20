using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretLaneSelector : MonoBehaviour
{
    private Button laneButton;
    [SerializeField] private FiveFretInstrument.LaneOrientation lane;

    private void Awake()
    {
        laneButton = GetComponent<Button>();
        laneButton.onClick.AddListener(TriggerSelectionUpdate);
    }

    private void TriggerSelectionUpdate() => Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionToNewLane(lane);
}