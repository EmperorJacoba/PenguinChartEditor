using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class WaveformSelectorDropdown : MonoBehaviour
{
    TMP_Dropdown dropdown;
    WaveformManager waveformManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dropdown = gameObject.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();

        SetUpWaveformOptions();
        dropdown.value = 0;
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    /// <summary>
    /// Contains the stems and their stored option order in the waveform selector dropdown.
    /// <para>Index 0 has a value of 0 for a "null" or invisible value, which signals that the waveform has been turned off. First option is always "none."</para>
    /// </summary>
    private List<ChartMetadata.StemType> dropdownIndexes = new();

    /// <summary>
    /// 
    /// </summary>
    private void SetUpWaveformOptions()
    {
        dropdown.options.Add(new TMP_Dropdown.OptionData("None"));
        dropdownIndexes.Add(0); // 0 is not valid StemType, but list will still accept it => used to show that waveform is inactive

        // Organize stems before adding to dropdown so the stem selection dropdown isn't a mess
        ChartMetadata.Stems = ChartMetadata.Stems.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

        // Add options to dropdown from StemType
        foreach (var entry in ChartMetadata.Stems)
        {
            var capitalizedEntry = Capitalize(entry.Key.ToString()); // for more polished look
            dropdown.options.Add(new TMP_Dropdown.OptionData(capitalizedEntry));
            dropdownIndexes.Add(entry.Key);
        }
    }

    private string Capitalize(string name)
    {
        return char.ToUpper(name[0]) + name.Substring(1); 
    }

    /// <summary>
    /// Method that handles changing visible waveform from dropdown selection.
    /// </summary>
    /// <param name="index"></param>
    private void OnValueChanged(int index)
    {
        if (Enum.IsDefined(typeof(ChartMetadata.StemType), index)) // value can be zero, but zero is not in StemType
        {
            waveformManager.ChangeDisplayedWaveform(dropdownIndexes[index]);
            // ^^ dropdownIndexes is used instead of just the index because the dropdown only contains stems the user has defined
            // ^^ so passed in index has to be converted to a StemType identifier 
        }
        else // index = 0, so "none" was chosen, so "disable" waveform
        {
            waveformManager.SetWaveformVisibility(false);
        }
    }
}
