using UnityEngine;
using TMPro;

public class CurrentTimeDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI SongTimestampLabel;
    [SerializeField] TMP_InputField TimeInputField;

    const float MINUTES_TO_SECONDS_CONVERSION = 60;
    const float MILLISECONDS_TO_SECONDS_CONVERSION = 1000;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateSongText();
        SongTime.TimeChanged += UpdateSongText;
        TimeInputField.onEndEdit.AddListener(x => PrepTimeEdit(x));
    }
    
    // Diagnostic: This function takes <0.05ms on average per frame during song playback.
    private void UpdateSongText()
    {
        SongTimestampLabel.text = Chart.SyncTrackInstrument.ConvertSecondsToTimestamp(SongTime.SongPositionSeconds);
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

    void PrepTimeEdit(string newTime)
    {
        try
        {
            HandleEndTimeEdit(newTime);
        }
        catch { } // this is here to avoid overflow errors
        TimeInputField.gameObject.SetActive(false);
        SongTime.ToggleChartingInputMap();
    }

    void HandleEndTimeEdit(string newTime)
    {
        int minutes = 0;
        int seconds = 0;
        int milliseconds = 0;

        bool noSplit = false;

        // Isolate minute value, if it exists
        try
        {
            var minSplit = newTime.Split(':');
            minutes = int.Parse(minSplit[0]);
            newTime = minSplit[1];
        }
        catch { noSplit = true; minutes = 0; } // minutes is set to zero to prevent doubling values when minutes is set to the first array val

        // Isolate second and millisecond value, if it exists
        try
        {
            var secSplit = newTime.Split('.');
            seconds = int.Parse(secSplit[0]);
            milliseconds = int.Parse(secSplit[1]);

            noSplit = false;
        }
        catch { noSplit = true; }

        // If no other time type/divider is present, interpret the raw number as seconds
        if (noSplit) seconds = int.Parse(newTime);

        // Convert and add together isolated values
        float newSecondValue = minutes * MINUTES_TO_SECONDS_CONVERSION + seconds + milliseconds / MILLISECONDS_TO_SECONDS_CONVERSION;

        SongTime.SongPositionSeconds = newSecondValue;
    }
}
