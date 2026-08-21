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

    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField entryBox;
    [SerializeField] private float minValue;
    [SerializeField] private float maxValue;
    [SerializeField] private float defaultStartingValue;

    /// <summary>
    /// The data type that this slider + input field combo changes.
    /// </summary>
    [SerializeField] private WaveformProperties dataType;

    /// <summary>
    /// Holds possible properties that the slider + input field combo can change. 
    /// </summary>
    private enum WaveformProperties
    {
        shrinkFactor = 0,
        amplitude = 1,
        playSpeed = 2,
        highwayLength = 3
    }

    private void Start()
    {
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        EntryBoxChange(GetStartingValue().ToString());

        slider.onValueChanged.AddListener(SliderChange);
        entryBox.onEndEdit.AddListener(EntryBoxChange);

        entryBox.text = slider.value.ToString();
    }

    /// <summary>
    /// Changes entry box and variable values upon slider value change.
    /// </summary>
    /// <param name="newValue"></param>
    private void SliderChange(float newValue)
    {
        if ((float.Parse(entryBox.text) > slider.maxValue || 
            float.Parse(entryBox.text) < slider.minValue) && 
            (newValue >= slider.maxValue ||
            newValue <= slider.minValue))
        {
            print("Early return");
            return;
        }

        // Prevent values in entry boxes from looking wonky with 100000 decimal places
        newValue = (float)Math.Round(newValue, 2);

        float entryBoxDisplay = newValue; // value put into variable will differ from what is displayed for hyperspeed/shrinkFactor
        entryBox.text = entryBoxDisplay.ToString();

        if (dataType == WaveformProperties.highwayLength)
        {
            entryBox.text = Math.Round(entryBoxDisplay).ToString();
        }

        UpdateValue(newValue);
    }

    private void EntryBoxChange(string newValue)
    {
        if (!float.TryParse(newValue, out var valueAsFloat) && (valueAsFloat <= 0 || valueAsFloat > VARIABLE_SAFETY_UPPER_BOUND))
        {
            entryBox.text = slider.value.ToString();
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

        UpdateValue(valueAsFloat);
    }

    private float GetStartingValue()
    {
        switch(dataType)
        {
            case WaveformProperties.shrinkFactor:
                return Waveform.ShrinkFactor * HYPERSPEED_TO_SHRINK_FACTOR_CONVERSION_FACTOR;
            case WaveformProperties.amplitude:
                return Waveform.Amplitude;
            case WaveformProperties.playSpeed:
                return AudioManager.AudioSpeed;
            case WaveformProperties.highwayLength:
                return Highway.highwayLength;
        }

        return defaultStartingValue;
    }

    private void UpdateValue(float newValue)
    {
        // Make sure we change the correct data!
        switch(dataType)
        {
            case WaveformProperties.shrinkFactor:
                // shrinkFactor is a decimal in the 0.001 to 0.01 range BUT that looks really ugly
                // so display a whole number and divide it to work with the code
                Waveform.ShrinkFactor = newValue / HYPERSPEED_TO_SHRINK_FACTOR_CONVERSION_FACTOR;
                break;
            case WaveformProperties.amplitude:
                Waveform.Amplitude = newValue;
                break;
            case WaveformProperties.playSpeed:
                AudioManager.AudioSpeed = newValue;
                break;
            case WaveformProperties.highwayLength:
                Highway.highwayLength = newValue;
                break;

        }
        Chart.SyncTrackInPlaceRefresh();
    }
}
