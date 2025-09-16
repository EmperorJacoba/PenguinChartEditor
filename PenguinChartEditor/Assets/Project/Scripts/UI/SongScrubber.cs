using UnityEngine;
using UnityEngine.UI;

public class SongScrubber : MonoBehaviour
{
    [SerializeField] Scrollbar scrubber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SongTimelineManager.TimeChanged += UpdateSongScrubber;

        scrubber.onValueChanged.AddListener(x => UpdateSongTimeFromScrubber(x));
    }
    
    void UpdateSongScrubber()
    {
        scrubber.value = (float)SongTimelineManager.SongPositionSeconds / AudioManager.SongLength;
    }

    void UpdateSongTimeFromScrubber(float newPos)
    {
        SongTimelineManager.SongPositionSeconds = AudioManager.SongLength * scrubber.value;
    }
}
