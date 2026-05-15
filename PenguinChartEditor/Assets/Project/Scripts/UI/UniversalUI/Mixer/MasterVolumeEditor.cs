using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

/// <summary>
/// The script attached to the prefab that contains volume controls for stems.
/// </summary>
public class MasterVolumeEditor : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField entryBox;

    private void Start()
    {
        slider.onValueChanged.AddListener(SliderChange);
        entryBox.onEndEdit.AddListener(EntryBoxChange);
    }

    /// <summary>
    /// Changes entry box and variable values upon slider value change.
    /// </summary>
    /// <param name="newValue"></param>
    private void SliderChange(float newValue)
    {
        // Prevent values in entry boxes from looking wonky with 100000 decimal places
        newValue = (float)Math.Round(newValue, 3);

        entryBox.text = newValue.ToString();

        AudioManager.MasterVolume = newValue;
    }

    /// <summary>
    /// Changes slider and variable values upon entry box value change.
    /// </summary>
    /// <param name="newValue"></param>
    private void EntryBoxChange(string newValue)
    {
        var valueAsFloat = ValidateEntryBoxText(newValue);
        slider.value = valueAsFloat;

        AudioManager.MasterVolume = valueAsFloat;
    }

    private float ValidateEntryBoxText(string text)
    {
        // Entry boxes should be decimal numerical only
        var valueAsFloat = float.Parse(text);

        // Clamp values to prevent illegal volumes
        if (valueAsFloat < 0)
        {
            valueAsFloat = 0;
        }
        else if (valueAsFloat > 1)
        {
            valueAsFloat = 1;
        }
        return valueAsFloat;
    }
}
