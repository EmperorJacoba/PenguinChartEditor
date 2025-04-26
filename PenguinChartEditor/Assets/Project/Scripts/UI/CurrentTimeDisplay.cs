using UnityEngine;
using TMPro;

public class CurrentTimeDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI SongTimestampLabel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateSongText();
        SongTimelineManager.TimeChanged += UpdateSongText;
    }

    private void UpdateSongText()
    {
        SongTimestampLabel.text = SongTimelineManager.ConvertSecondsToTimestamp(SongTimelineManager.SongPositionSeconds);
    }
}
