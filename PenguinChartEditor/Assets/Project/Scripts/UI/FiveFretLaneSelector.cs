using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretLaneSelector : MonoBehaviour
{
    Button laneButton;
    [SerializeField] FiveFretInstrument.LaneOrientation lane;

    private void Awake()
    {
        laneButton = GetComponent<Button>();
        laneButton.onClick.AddListener(TriggerSelectionUpdate);
    }

    void TriggerSelectionUpdate() => Chart.GetActiveInstrument<FiveFretInstrument>().SetSelectionToNewLane(lane);
}