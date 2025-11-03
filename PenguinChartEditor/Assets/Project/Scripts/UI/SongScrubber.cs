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
    
    void UpdateSongScrubber()
    {
        scrubber.value = (float)SongTime.SongPositionSeconds / AudioManager.SongLength;
    }

    void UpdateSongTimeFromScrubber(float newPos)
    {
        SongTime.SongPositionSeconds = AudioManager.SongLength * scrubber.value;
    }
}
