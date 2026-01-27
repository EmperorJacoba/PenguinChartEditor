using UnityEngine;
using TMPro;

public class TotalTimeDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI SongLengthLabel;

    private void Start()
    {
        UpdateSongLengthText();
    }

    private void UpdateSongLengthText()
    {
        SongLengthLabel.text = Chart.SyncTrackInstrument.ConvertSecondsToTimestamp(AudioManager.SongLength);
    }
}
