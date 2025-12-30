using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class SoloPlacementController : MonoBehaviour
{
    Toggle toggle;
    InputMap inputMap;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(x => UserSettings.SoloPlacingAllowed = x);
        toggle.isOn = UserSettings.SoloPlacingAllowed;

        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ToggleSolos.performed += x =>
        {
            toggle.isOn = !toggle.isOn;
        };
    }
}