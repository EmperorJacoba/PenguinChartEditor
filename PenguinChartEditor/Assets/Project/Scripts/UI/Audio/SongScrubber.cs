using UnityEngine;
using UnityEngine.UI;

public class SongScrubber : MonoBehaviour
{
    [SerializeField] Scrollbar scrubber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SongTime.TimeChanged += UpdateSongScrubber;

        scrubber.onValueChanged.AddListener(x => UpdateSongTimeFromScrubber(x));
        AudioManager.PlaybackStateChanged += (playbackState =>
        {
            if (playbackState) scrubber.interactable = false;
            else scrubber.interactable = true;
        });
    }
    
    // Diagnostic: This function takes <0.05ms on average per frame during song playback.
    void UpdateSongScrubber()
    {
        scrubber.value = (float)SongTime.SongPositionSeconds / AudioManager.SongLength;
    }

    void UpdateSongTimeFromScrubber(float newPos)
    {
        // Don't worry about dual updates for a scroll - SongPositionSeconds will not update the scene unless the passed in value is different
        SongTime.SongPositionSeconds = AudioManager.SongLength * scrubber.value;
    }
}
