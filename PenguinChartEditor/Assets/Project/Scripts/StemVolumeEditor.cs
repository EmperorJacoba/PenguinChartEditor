using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// The script attached to the prefab that contains volume controls for stems.
/// </summary>
public class StemVolumeEditor : MonoBehaviour
{
    public ChartMetadata.StemType StemType
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
    private ChartMetadata.StemType _type;

    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] TMP_InputField entryBox;

    void Start()
    {
        slider.onValueChanged.AddListener(x => SliderChange(x));
        entryBox.onEndEdit.AddListener(x => EntryBoxChange(x));
    }
    /// <summary>
    /// Changes entry box and variable values upon slider value change.
    /// </summary>
    /// <param name="newValue"></param>
    void SliderChange(float newValue)
    {
        // Prevent values in entry boxes from looking wonky with 100000 decimal places
        newValue = (float)Math.Round(newValue, 3);

        entryBox.text = newValue.ToString();

        PluginBassManager.UpdateStemVolume(StemType, newValue);
    }

    /// <summary>
    /// Changes slider and variable values upon entry box value change.
    /// </summary>
    /// <param name="newValue"></param>
    void EntryBoxChange(string newValue)
    {
        // Entry boxes should be decimal numerical only
        var valueAsFloat = float.Parse(newValue);

        // Clamp values to prevent illegal volumes
        if (valueAsFloat < 0)
        {
            valueAsFloat = 0;
        }
        else if (valueAsFloat > 1)
        {
            valueAsFloat = 1;
        }

        slider.value = valueAsFloat;

        PluginBassManager.UpdateStemVolume(StemType, valueAsFloat);
    }
}
