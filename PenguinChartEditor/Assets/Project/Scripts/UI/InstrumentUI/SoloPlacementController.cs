using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SoloPlacementController : MonoBehaviour
{
    private Toggle toggle;

    private void ToggleSolos(InputAction.CallbackContext _)
    {
        toggle.isOn = !toggle.isOn;
    }

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(x => Chart.settings.SoloPlacingAllowed = x);
        toggle.isOn = Chart.settings.SoloPlacingAllowed;

        Chart.inputMap.UIShortcuts.ToggleSolos.performed += ToggleSolos;
    }

    private void OnDestroy()
    {
        Chart.inputMap.UIShortcuts.ToggleSolos.performed -= ToggleSolos;
    }
}