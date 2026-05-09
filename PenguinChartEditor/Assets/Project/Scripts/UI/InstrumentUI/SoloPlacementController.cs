using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SoloPlacementController : MonoBehaviour
{
    private Toggle toggle;
    private InputMap inputMap;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(x => Chart.settings.SoloPlacingAllowed = x);
        toggle.isOn = Chart.settings.SoloPlacingAllowed;

        inputMap = new InputMap();
        inputMap.Enable();

        inputMap.Charting.ToggleSolos.performed += x =>
        {
            toggle.isOn = !toggle.isOn;
        };
    }

    private void OnDestroy()
    {
        inputMap.Disable();
    }
}