using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script attached to any slider / input field combination meant to change a waveform property.
/// <para>Updates components' values based on one another as well as the data itself.</para>
/// </summary>
public class SliderBoxLinker : MonoBehaviour
{
    private const int VARIABLE_SAFETY_UPPER_BOUND = 10000;
    private const int HYPERSPEED_TO_SHRINK_FACTOR_CONVERSION_FACTOR = 1000;

    [SerializeField] Slider slider;
    [SerializeField] TMP_InputField entryBox;

    /// <summary>
    /// The data type that this slider + input field combo changes.
    /// </summary>
    [SerializeField] WaveformProperties dataType;

    /// <summary>
    /// Holds possible properties that the slider + input field combo can change. 
    /// </summary>
    enum WaveformProperties
    {
        shrinkFactor = 0,
        amplitude = 1,
        playSpeed = 2
    }

    void Awake()
    {
        slider.onValueChanged.AddListener(x => SliderChange(x));
        entryBox.onEndEdit.AddListener(x => EntryBoxChange(x));
    }

    void Start()
    {
        // Change this once importing from previous sessions is added
        entryBox.text = slider.value.ToString();
    }

    /// <summary>
    /// Changes entry box and variable values upon slider value change.
    /// </summary>
    /// <param name="newValue"></param>
    void SliderChange(float newValue)
    {
        if (newValue < slider.minValue || newValue > slider.maxValue)
        {
            return;
        }
        // Prevent values in entry boxes from looking wonky with 100000 decimal places
        newValue = (float)Math.Round(newValue, 3);

        float entryBoxDisplay = newValue; // value put into variable will differ from what is displayed for hyperspeed/shrinkFactor
        entryBox.text = entryBoxDisplay.ToString();

        // shrinkFactor is a decimal in the 0.001 to 0.01 range BUT that looks really ugly
        // so display a whole number and divide it to work with the code
        if (dataType == WaveformProperties.shrinkFactor)
        {
            newValue /= HYPERSPEED_TO_SHRINK_FACTOR_CONVERSION_FACTOR; 
        }

        UpdateValue(newValue);
    }

    /// <summary>
    /// Changes slider and variable values upon entry box value change.
    /// </summary>
    /// <param name="newValue"></param>
    void EntryBoxChange(string newValue)
    {
        // Entry boxes should be decimal numerical only
        var valueAsFloat = float.Parse(newValue);

        // Anything >10000 seems to make unity die so let's not do that
        // I like insane customizability but I really don't want someone to lose their work because they set hyperspeed to 100,000,000
        if (valueAsFloat > VARIABLE_SAFETY_UPPER_BOUND)
        {
            valueAsFloat = VARIABLE_SAFETY_UPPER_BOUND;
        }

        // This will cause div by zero errors if not here
        if (valueAsFloat == 0)
        {
            return;
        }

        // Clamp slider value displayed but still allow for values outside the slider bounds
        if (valueAsFloat > slider.maxValue)
        {
            slider.value = slider.maxValue;
        }
        else if (valueAsFloat < slider.minValue)
        {
            slider.value = slider.minValue;
        }
        else
        {
            slider.value = valueAsFloat;
        }

        // shrinkFactor is a decimal in the 0.001 to 0.01 range BUT that looks really ugly
        // so display a whole number and divide it to work with the code
        if (dataType == WaveformProperties.shrinkFactor)
        {
            valueAsFloat /= HYPERSPEED_TO_SHRINK_FACTOR_CONVERSION_FACTOR; 
        }

        UpdateValue(valueAsFloat);
    }

    void UpdateValue(float newValue)
    {
        // Make sure we change the correct data!
        switch(dataType)
        {
            case WaveformProperties.shrinkFactor:
                WaveformManager.ShrinkFactor = newValue;
                break;
            case WaveformProperties.amplitude:
                WaveformManager.Amplitude = newValue;
                break;
            case WaveformProperties.playSpeed:
                PluginBassManager.PlaySpeed = newValue;
                break;
        } 
    }
}
