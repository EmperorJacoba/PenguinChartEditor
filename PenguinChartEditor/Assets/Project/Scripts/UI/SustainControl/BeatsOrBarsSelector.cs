using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class BeatsOrBarsSelector : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    public enum JumpLengthOptions
    {
        Beats = 0,
        Bars = 1
    }
    
    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.options = new List<TMP_Dropdown.OptionData>()
            { 
                new TMP_Dropdown.OptionData("beats"), 
                new TMP_Dropdown.OptionData("bars") 
            };
    }

    public JumpLengthOptions GetUnitSelectionMode() => (JumpLengthOptions)dropdown.value;
}