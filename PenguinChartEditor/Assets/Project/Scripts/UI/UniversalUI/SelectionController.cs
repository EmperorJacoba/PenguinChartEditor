using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Attach to selection dropdown to avoid going through scene's temporary chart instance. 
/// Chart instances in scenes destroy themselves in favor of the current chart object (with all the information in it),
/// so referencing the temporary testing scene object will not work when built.
/// </summary>
public class SelectionController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    private void Awake()
    {
        Chart.instance.inputMap.Selections.SelectionView.performed += SetSelectionView;
        Chart.instance.inputMap.Selections.SelectionEdit.performed += SetSelectionEdit;
        Chart.instance.inputMap.Selections.SelectionSelect.performed += SetSelectionSelect; 

        dropdown.value = (int)Chart.currentSelectionMode;
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(int index)
    {
        Chart.currentSelectionMode = (Chart.SelectionMode)index;
    }

    private void SetSelectionView(InputAction.CallbackContext _) => SetSelectionMode(Chart.SelectionMode.View);
    private void SetSelectionEdit(InputAction.CallbackContext _) => SetSelectionMode(Chart.SelectionMode.Edit);
    private void SetSelectionSelect(InputAction.CallbackContext _) => SetSelectionMode(Chart.SelectionMode.Select);
    private void SetSelectionMode(Chart.SelectionMode mode)
    {
        dropdown.value = (int)mode;
        Chart.currentSelectionMode = mode;
    }
}
