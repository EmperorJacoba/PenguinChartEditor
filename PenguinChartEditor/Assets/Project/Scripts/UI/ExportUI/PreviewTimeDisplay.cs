using System;
using TMPro;
using UnityEngine;

public class PreviewTimeDisplay : MonoBehaviour
{
    private TMP_InputField attachedInputField;

    private void Awake()
    {
        Chart.ChartFileLoaded += OnEnable;
    }

    private void OnEnable()
    {
        attachedInputField = GetComponent<TMP_InputField>();
        UpdatePreviewDisplay();
        
        Chart.Metadata.PreviewStartTimeUpdated += UpdatePreviewDisplay;
        attachedInputField.onEndEdit.AddListener(SetPreviewStartTime);
    }

    private void OnDisable()
    {
        Chart.Metadata.PreviewStartTimeUpdated -= UpdatePreviewDisplay;
    }

    private void UpdatePreviewDisplay()
    {
        attachedInputField.text = SongTime.ConvertSecondsToTimestamp(Chart.Metadata.PreviewStartTime);
    }

    private static void SetPreviewStartTime(string text)
    {
        Chart.Metadata.PreviewStartTime = SongTime.ConvertFormattedTimestampToSeconds(text);
    }
}