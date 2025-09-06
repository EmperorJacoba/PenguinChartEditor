using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;
using System.Linq;

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
    [SerializeField] Button muteButton;
    [SerializeField] Button soloButton;

    enum ButtonStates
    {
        normal,
        muted,
        soloed
    }

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

        PluginBassManager.SetStemVolume(StemType, newValue);
    }

    /// <summary>
    /// Changes slider and variable values upon entry box value change.
    /// </summary>
    /// <param name="newValue"></param>
    void EntryBoxChange(string newValue)
    {
        var valueAsFloat = ValidateEntryBoxText(newValue);
        slider.value = valueAsFloat;

        PluginBassManager.SetStemVolume(StemType, valueAsFloat);
    }

    float ValidateEntryBoxText(string text)
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

    public void OnMuteButtonPress()
    {
        if (PluginBassManager.soloedStems.Count > 0)
        {
            return;
        }
        
        if (PluginBassManager.StemVolumes[StemType].Muted)
        {
            // unmute stream, change volume to new value
            PluginBassManager.UnmuteStem(StemType);
            UpdateButtonState(muteButton, ButtonStates.normal);
        }
        else
        {
            PluginBassManager.MuteStem(StemType);
            UpdateButtonState(muteButton, ButtonStates.muted);
        }
    }

    void UpdateButtonState(Button targetButton, ButtonStates newState)
    {
        switch (newState)
        {
            case ButtonStates.normal:
                targetButton.image.color = Color.white;
                break;
            case ButtonStates.muted:
                targetButton.image.color = Color.yellow;
                break;
            case ButtonStates.soloed:
                targetButton.image.color = Color.red;
                break;
        }
    }

    public void OnSoloButtonPress()
    {
        // mute all other stems except this one
        // if this stem is muted, unmute it
        // when unsoloing, check to make sure there is <2 stems unmuted
        // if >=2 stems unmuted, just mute single stem

        if (PluginBassManager.soloedStems.Contains(StemType))
        {
            PluginBassManager.soloedStems.Remove(StemType);
            if (PluginBassManager.soloedStems.Count > 0)
            {
                PluginBassManager.MuteStem(StemType);
            }
            else
            {
                foreach (var stem in PluginBassManager.StemVolumes.Keys.ToList())
                {
                    PluginBassManager.UnmuteStem(stem);
                }
            }

            // unmute all other stems, unless soloed list is >1, when you should instead mute the current stem
            UpdateButtonState(soloButton, ButtonStates.normal);
        }
        else
        {
            if (PluginBassManager.soloedStems.Count == 0)
            {
                foreach (var stem in PluginBassManager.StemVolumes.Keys.ToList())
                {
                    PluginBassManager.MuteStem(stem);
                }
            }

            PluginBassManager.UnmuteStem(StemType);
            PluginBassManager.soloedStems.Add(StemType);
            // mute all other stems, except those in the soloed list
            UpdateButtonState(soloButton, ButtonStates.soloed);
            UpdateButtonState(muteButton, ButtonStates.normal);
        }
    }
}
