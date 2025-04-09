using UnityEngine.UI;
using UnityEngine;
using TMPro;
using Unity.Mathematics;
using System;

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

    [SerializeField] Scrollbar SongScrubber;

    private void Awake() 
    {
        PluginBassManager.PlaybackStateChanged += state => ManagePlaybackButtonStates(state);
        SongTimelineManager.TimeChanged += UpdateSongText;
        SongTimelineManager.TimeChanged += UpdateSongScrubber;

        SongScrubber.onValueChanged.AddListener(x => UpdateSongTimeFromScrubber(x));
    }

    private void Start() 
    {
        ManagePlaybackButtonStates(false);
        UpdateSongLengthText();
        UpdateSongText();
    }

    void Update()
    {
        // These two triggers use event triggers, not default button functionality. 
        // Check for interactability or you can use them even when uninteractable
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

    private void ManagePlaybackButtonStates(bool playbackState)
    {
        // Set button interactiblity based on the current playback state (flip bools where needed)
        PlayButton.interactable = !playbackState;
        PauseButton.interactable = playbackState;
        FFWButton.interactable = !playbackState;
        RWButton.interactable = !playbackState;
        // Stop button stays the same (just a generic reset button)
    }
    
    void UpdateSongScrubber()
    {
        var ratio = (float)SongTimelineManager.SongPositionSeconds / PluginBassManager.SongLength;
        if (SongScrubber.value != ratio)
        {
            SongScrubber.value = ratio;
        }
    }

    void UpdateSongTimeFromScrubber(float newPos)
    {
        SongTimelineManager.SongPositionSeconds = PluginBassManager.SongLength * SongScrubber.value;
    }

    private void UpdateSongText()
    {
        SongTimestampLabel.text = ConvertSecondsToTimestamp(SongTimelineManager.SongPositionSeconds);
    }

    private void UpdateSongLengthText()
    {
        SongLengthLabel.text = ConvertSecondsToTimestamp(PluginBassManager.SongLength);
    }

    private string ConvertSecondsToTimestamp(double position)
    {
        var minutes = Math.Floor(position / 60);
        var secondsWithMS = position - minutes * 60;
        var seconds = (int)Math.Floor(secondsWithMS);
        var milliseconds = Math.Round(secondsWithMS - seconds, 3) * 1000;

        string minutesString = minutes.ToString();
        if (minutes < 10)
        {
            minutesString = minutesString.PadLeft(minutesString.Length + 1, '0');
        }

        string secondsString = seconds.ToString();
        if (seconds < 10)
        {
            secondsString = secondsString.PadLeft(2, '0');
        }

        string millisecondsString = milliseconds.ToString();
        if (millisecondsString.Length < 3)
        {
            millisecondsString = millisecondsString.PadRight(3, '0');
        }

        return minutesString + ":" + secondsString + "." + millisecondsString;
    }
}
