using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

/// <summary>
/// The script attached to the prefab that contains volume controls for stems.
/// </summary>
public class SFXVolumeEditor : MonoBehaviour
{
    public AudioManager.SFX ControlledSFX
    {
        get
        {
            return _type;
        }
        set
        {
            // Automatically update the package's label when the stem type is set
            label.text = MiscTools.Capitalize(value.ToString());
            _type = value;
        }
    }
    private AudioManager.SFX _type;

    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI label;
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

        AudioManager.SetSFXVolume(ControlledSFX, newValue);
    }

    /// <summary>
    /// Changes slider and variable values upon entry box value change.
    /// </summary>
    /// <param name="newValue"></param>
    private void EntryBoxChange(string newValue)
    {
        var valueAsFloat = ValidateEntryBoxText(newValue);
        slider.value = valueAsFloat;

        AudioManager.SetSFXVolume(ControlledSFX, valueAsFloat);
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
        else if (valueAsFloat > 2)
        {
            valueAsFloat = 2;
        }
        return valueAsFloat;
    }
}
