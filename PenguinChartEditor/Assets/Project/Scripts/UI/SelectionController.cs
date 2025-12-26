using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to selection dropdown to avoid going through scene's temporary chart instance. 
/// Chart instances in scenes destroy themselves in favor of the current chart object (with all the information in it),
/// so referencing the temporary testing scene object will not work when built.
/// </summary>
public class SelectionController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    InputMap inputMap;
    private void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.SelectionView.performed += x => SetSelectionMode(Chart.SelectionMode.view);
        inputMap.Charting.SelectionEdit.performed += x => SetSelectionMode(Chart.SelectionMode.edit);
        inputMap.Charting.SelectionSelect.performed += x => SetSelectionMode(Chart.SelectionMode.select);

        dropdown.value = (int)Chart.currentSelectionMode;
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(int index)
    {
        Chart.currentSelectionMode = (Chart.SelectionMode)index;
    }

    void SetSelectionMode(Chart.SelectionMode mode)
    {
        dropdown.value = (int)mode;
        Chart.currentSelectionMode = mode;
    }
}
