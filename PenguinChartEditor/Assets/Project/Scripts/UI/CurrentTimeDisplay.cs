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
        SongTimelineManager.TimeChanged += UpdateSongText;
        TimeInputField.onEndEdit.AddListener(x => PrepTimeEdit(x));
    }

    private void UpdateSongText()
    {
        SongTimestampLabel.text = SongTimelineManager.ConvertSecondsToTimestamp(SongTimelineManager.SongPositionSeconds);
    }

    public void ActivateManualEntry()
    {
        TimeInputField.gameObject.SetActive(true);

        TimeInputField.text = SongTimestampLabel.text;

        TimeInputField.ActivateInputField();
    }

    void PrepTimeEdit(string newTime)
    {
        try
        {
            HandleEndTimeEdit(newTime);
        }
        catch { }
        TimeInputField.gameObject.SetActive(false);
    }
    void HandleEndTimeEdit(string newTime)
    {
        // take new string
        // format it to seconds
        // set songpositionseconds to new time
        int minutes = 0;
        int seconds = 0;
        int milliseconds = 0;

        bool noSplit = false;

        try
        {
            var minSplit = newTime.Split(':');
            minutes = int.Parse(minSplit[0]);
            newTime = minSplit[1];
        }
        catch { noSplit = true; minutes = 0; }

        try
        {
            var secSplit = newTime.Split('.');
            seconds = int.Parse(secSplit[0]);
            milliseconds = int.Parse(secSplit[1]);

            noSplit = false;
        }
        catch { noSplit = true; }

        if (noSplit) seconds = int.Parse(newTime);

        float newSecondValue = minutes * MINUTES_TO_SECONDS_CONVERSION + seconds + milliseconds / MILLISECONDS_TO_SECONDS_CONVERSION;

        SongTimelineManager.SongPositionSeconds = newSecondValue;
    }
}
