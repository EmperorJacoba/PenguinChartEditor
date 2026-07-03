using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SoloPlacementController : MonoBehaviour
{
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(x => Chart.settings.SoloPlacingAllowed = x);
        toggle.isOn = Chart.settings.SoloPlacingAllowed;
        
        Chart.inputMap.UIShortcuts.ToggleSolos.performed += x =>
        {
            toggle.isOn = !toggle.isOn;
        };
    }
}