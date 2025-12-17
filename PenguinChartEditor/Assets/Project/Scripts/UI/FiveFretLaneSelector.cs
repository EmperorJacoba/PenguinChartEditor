using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FiveFretLaneSelector : MonoBehaviour
{
    [SerializeField] Button laneButton;
    [SerializeField] FiveFretInstrument.LaneOrientation lane;
    FiveFretInstrument ActiveInstrument => (FiveFretInstrument)Chart.LoadedInstrument;

    private void Awake()
    {
        laneButton.onClick.AddListener(TriggerSelectionUpdate);
    }

    void TriggerSelectionUpdate() => ActiveInstrument.SetSelectionToNewLane(lane);
}