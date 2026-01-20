using UnityEngine;
using TMPro;

public class TotalTimeDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI SongLengthLabel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        UpdateSongLengthText();
    }

    private void UpdateSongLengthText()
    {
        SongLengthLabel.text = Chart.SyncTrackInstrument.ConvertSecondsToTimestamp(AudioManager.SongLength);
    }
}
