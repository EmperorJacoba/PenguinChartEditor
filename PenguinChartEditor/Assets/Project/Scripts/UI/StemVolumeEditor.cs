using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

/// <summary>
/// The script attached to the prefab that contains volume controls for stems.
/// </summary>
public class StemVolumeEditor : MonoBehaviour
{
    public StemType StemType
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
    private StemType _type;

    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TMP_InputField entryBox;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button soloButton;

    private delegate void UnsoloDelegate();

    private static event UnsoloDelegate SoloedStemOccurred;

    private enum ButtonStates
    {
        normal,
        muted,
        soloed
    }

    private void Start()
    {
        SoloedStemOccurred += UpdateMuteButton;

        slider.onValueChanged.AddListener(x => SliderChange(x));
        entryBox.onEndEdit.AddListener(x => EntryBoxChange(x));
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

        AudioManager.SetStemVolume(StemType, newValue);
    }

    /// <summary>
    /// Changes slider and variable values upon entry box value change.
    /// </summary>
    /// <param name="newValue"></param>
    private void EntryBoxChange(string newValue)
    {
        var valueAsFloat = ValidateEntryBoxText(newValue);
        slider.value = valueAsFloat;

        AudioManager.SetStemVolume(StemType, valueAsFloat);
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

    public void OnMuteButtonPress()
    {
        if (AudioManager.soloedStems.Count > 0)
        {
            return;
        }
        
        if (AudioManager.StemVolumes[StemType].Muted)
        {
            AudioManager.UnmuteStem(StemType);
            UpdateButtonState(muteButton, ButtonStates.normal);
        }
        else
        {
            AudioManager.MuteStem(StemType);
            UpdateButtonState(muteButton, ButtonStates.muted);
        }
    }

    private void UpdateButtonState(Button targetButton, ButtonStates newState)
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

    private void UpdateMuteButton()
    {
        UpdateButtonState(muteButton, ButtonStates.normal);
    }

    public void OnSoloButtonPress()
    {
        if (AudioManager.soloedStems.Contains(StemType))
        {
            AudioManager.soloedStems.Remove(StemType);
            if (AudioManager.soloedStems.Count > 0)
            {
                AudioManager.MuteStem(StemType);
            }
            else
            {
                // weird workaround is needed for this for loop with the list
                // b/c C# doesn't like it when you edit a <K, <struct>> dictionary
                // while you're enumerating through it
                foreach (var stem in AudioManager.StemVolumes.Keys.ToList())
                {
                    AudioManager.UnmuteStem(stem);
                }
            }

            UpdateButtonState(soloButton, ButtonStates.normal);
        }
        else
        {
            if (AudioManager.soloedStems.Count == 0)
            {
                foreach (var stem in AudioManager.StemVolumes.Keys.ToList())
                {
                    AudioManager.MuteStem(stem);
                }
                SoloedStemOccurred?.Invoke(); // undo any active mutes
            }

            AudioManager.UnmuteStem(StemType);
            AudioManager.soloedStems.Add(StemType);

            UpdateButtonState(soloButton, ButtonStates.soloed);
            UpdateButtonState(muteButton, ButtonStates.normal);
        }
    }
}
