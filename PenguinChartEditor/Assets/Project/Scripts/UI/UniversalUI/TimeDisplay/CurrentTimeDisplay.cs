using UnityEngine;
using TMPro;

public class CurrentTimeDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI SongTimestampLabel;
    [SerializeField] private TMP_InputField TimeInputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        UpdateSongText();
        SongTime.TimeChanged += UpdateSongText;
        TimeInputField.onEndEdit.AddListener(PrepTimeEdit);
    }
    
    // Diagnostic: This function takes <0.05ms on average per frame during song playback.
    private void UpdateSongText()
    {
        SongTimestampLabel.text = SongTime.ConvertSecondsToTimestamp(SongTime.SongPositionSeconds);
    }

    /// <summary>
    /// Upon clicking the invisible button on the timestamp, activate this function to show the input field for manual entry
    /// </summary>
    public void ActivateManualEntry()
    {
        TimeInputField.gameObject.SetActive(true);

        TimeInputField.text = SongTimestampLabel.text;

        TimeInputField.ActivateInputField();

        SongTime.ToggleChartingInputMap();
    }

    private void PrepTimeEdit(string newTime)
    {
        SongTime.UpdateSongTimestampFromFormattedTimestamp(newTime);
        
        TimeInputField.gameObject.SetActive(false);
        SongTime.ToggleChartingInputMap();
    }
}
