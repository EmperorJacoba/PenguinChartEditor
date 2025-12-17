using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretLaneSelector : MonoBehaviour
{
    Button laneButton;
    [SerializeField] FiveFretInstrument.LaneOrientation lane;
    FiveFretInstrument ActiveInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    private void Awake()
    {
        laneButton = GetComponent<Button>();
        laneButton.onClick.AddListener(TriggerSelectionUpdate);
    }

    void TriggerSelectionUpdate() => ActiveInstrument.SetSelectionToNewLane(lane);
}