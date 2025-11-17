using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class WaveformSelectorDropdown : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] Waveform waveformManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dropdown.ClearOptions();

        SetUpWaveformOptions();
        dropdown.value = 0;
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    /// <summary>
    /// Contains the stems and their stored option order in the waveform selector dropdown.
    /// <para>Index 0 has a value of 0 for a "null" or invisible value, which signals that the waveform has been turned off. First option is always "none."</para>
    /// </summary>
    private List<StemType> dropdownIndexes = new();

    /// <summary>
    /// 
    /// </summary>
    private void SetUpWaveformOptions()
    {
        dropdown.options.Add(new TMP_Dropdown.OptionData("None"));
        dropdownIndexes.Add(0); // 0 is not valid StemType, but list will still accept it => used to show that waveform is inactive

        // Organize stems before adding to dropdown so the stem selection dropdown isn't a mess
        Chart.Metadata.StemPaths = Chart.Metadata.StemPaths.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

        // Add options to dropdown from StemType
        foreach (var entry in Chart.Metadata.StemPaths)
        {
            var capitalizedEntry = MiscTools.Capitalize(entry.Key.ToString()); // for more polished look
            dropdown.options.Add(new TMP_Dropdown.OptionData(capitalizedEntry));
            dropdownIndexes.Add(entry.Key);
        }
    }



    /// <summary>
    /// Method that handles changing visible waveform from dropdown selection.
    /// </summary>
    /// <param name="index"></param>
    private void OnValueChanged(int index)
    {
        if (Enum.IsDefined(typeof(StemType), index)) // value can be zero, but zero is not in StemType
        {
            waveformManager.Visible = true;

            // dropdownIndexes is used instead of just the index because the dropdown only contains stems the user has defined
            // so passed in index has to be converted to a StemType identifier 
            waveformManager.ChangeDisplayedWaveform(dropdownIndexes[index]);
        }
        else // index = 0, so "none" was chosen, so "disable" waveform
        {
            waveformManager.Visible = false;
        }
    }
}
