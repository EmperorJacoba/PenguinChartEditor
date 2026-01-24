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
    [SerializeField] private TMP_Dropdown dropdown;
    private InputMap inputMap;
    private void Awake()
    {
        inputMap = new InputMap();
        inputMap.Enable();

        inputMap.Charting.SelectionView.performed += x => SetSelectionMode(Chart.SelectionMode.View);
        inputMap.Charting.SelectionEdit.performed += x => SetSelectionMode(Chart.SelectionMode.Edit);
        inputMap.Charting.SelectionSelect.performed += x => SetSelectionMode(Chart.SelectionMode.Select);

        dropdown.value = (int)Chart.currentSelectionMode;
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(int index)
    {
        Chart.currentSelectionMode = (Chart.SelectionMode)index;
    }

    private void SetSelectionMode(Chart.SelectionMode mode)
    {
        dropdown.value = (int)mode;
        Chart.currentSelectionMode = mode;
    }
}
