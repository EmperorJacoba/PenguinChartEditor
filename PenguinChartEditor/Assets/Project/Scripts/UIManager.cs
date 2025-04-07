using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;
    [SerializeField] Button PlayButton;
    [SerializeField] Button PauseButton;
    [SerializeField] Button RWButton;
    [SerializeField] Button FFWButton;
    [SerializeField] Button StopButton;

    public bool RWButtonDown {get; set;}
    public bool FFWButtonDown {get; set;}

    [SerializeField] TextMeshProUGUI SongTimestampLabel;
    [SerializeField] TextMeshProUGUI SongLengthLabel;

    [SerializeField] Slider HyperspeedSlider;
    [SerializeField] TMP_InputField HyperspeedInput;

    [SerializeField] Slider AmplitudeSlider;
    [SerializeField] TMP_InputField AmplitudeInput;

    [SerializeField] Slider PlaybackSpeedSlider;
    [SerializeField] TMP_InputField PlaybackSpeedInput;

    [SerializeField] TMP_InputField DivisionInput;
    [SerializeField] Button IncreaseDivisionButton;
    [SerializeField] Button DecreaseDivisionButton;

    private void Awake() 
    {
        PluginBassManager.PlaybackStateChanged += state => ManagePlaybackButtonStates(state);
    }

    private void Start() 
    {
        ManagePlaybackButtonStates(false);    
    }

    void Update()
    {
        if (RWButtonDown && RWButton.interactable)
        {
            SongTimelineManager.ChangeTime(-UserSettings.ButtonScrollSensitivity);
        }

        if (FFWButtonDown && FFWButton.interactable)
        {
            SongTimelineManager.ChangeTime(UserSettings.ButtonScrollSensitivity);
        }
    }

    public void Play()
    {
        pluginBassManager.PlayAudio();
    }

    public void Pause()
    {
        pluginBassManager.PauseAudio();
    }

    public void Stop()
    {
        pluginBassManager.StopAudio();
    }

    public void RW()
    {

    }
    

    private void ManagePlaybackButtonStates(bool playbackState)
    {
        // Set button interactiblity based on the current playback state (flip bools where needed)
        PlayButton.interactable = !playbackState;
        PauseButton.interactable = playbackState;
        FFWButton.interactable = !playbackState;
        RWButton.interactable = !playbackState;
        // Stop button stays the same (just a generic reset button)
    }
}
