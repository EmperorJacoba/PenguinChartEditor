using UnityEngine;
using UnityEngine.UI;

public class SongScrubber : MonoBehaviour
{
    [SerializeField] private Scrollbar scrubber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
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
    private void UpdateSongScrubber()
    {
        disableNextUpdate = true;

        scrubber.value = (float)SongTime.SongPositionSeconds / AudioManager.SongLength;
        
        // onValueChanged is still invocated when this function is called and will
        // cause a dual refresh on the same frame from UpdateSongTimeFromScrubber.
        // this doubles compute time needed and thus must be prevented.
    }

    private bool disableNextUpdate = false;

    private void UpdateSongTimeFromScrubber(float newPos)
    {
        if (disableNextUpdate)
        {
            disableNextUpdate = false;
            return;
        }

        var newTime = AudioManager.SongLength * scrubber.value;
        if (SongTime.SongPositionSeconds == newTime) return;

        SongTime.SongPositionSeconds = newTime;
        Chart.InPlaceRefresh();
    }
}
