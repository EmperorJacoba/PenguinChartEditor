using UnityEngine;
using UnityEngine.UI;

public class AudioNavigationButtons : MonoBehaviour
{
    [SerializeField] Button PlayButton;
    [SerializeField] Button PauseButton;
    [SerializeField] Button RWButton;
    [SerializeField] Button FFWButton;

    public bool RWButtonDown {get; set;}
    public bool FFWButtonDown {get; set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.PlaybackStateChanged += state => ManagePlaybackButtonStates(state);
        ManagePlaybackButtonStates(false);
    }

    // Update is called once per frame
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
