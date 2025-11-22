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
    private void Awake()
    {
        dropdown.value = (int)Chart.currentSelectionMode;
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(int index)
    {
        Chart.currentSelectionMode = (Chart.SelectionMode)index;
    }
}
