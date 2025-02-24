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

    private void SetUpWaveformOptions()
    {
        dropdown.options.Add(new TMP_Dropdown.OptionData("None"));
        dropdownIndexes.Add(0);

        ChartMetadata.Stems = ChartMetadata.Stems.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var entry in ChartMetadata.Stems)
        {
            var capitalEntry = Capitalize(entry.Key.ToString());
            dropdown.options.Add(new TMP_Dropdown.OptionData(capitalEntry));
            dropdownIndexes.Add(entry.Key);
        }
    }

    private string Capitalize(string name)
    {
        return char.ToUpper(name[0]) + name.Substring(1);
    }

    private void OnValueChanged(int index)
    {
        if (Enum.IsDefined(typeof(ChartMetadata.StemType), index))
        {
            waveformManager.ChangeDisplayedWaveform(dropdownIndexes[index]);
        }
        else
        {
            waveformManager.ChangeWaveformVisibility(false);
        }
        
    }
}
