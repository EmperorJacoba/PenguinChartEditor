using UnityEngine;
using TMPro;

public class TotalTimeDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI SongLengthLabel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateSongLengthText();
    }

    private void UpdateSongLengthText()
    {
        SongLengthLabel.text = Tempo.ConvertSecondsToTimestamp(AudioManager.SongLength);
    }
}
