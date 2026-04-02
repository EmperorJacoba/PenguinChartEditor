using UnityEngine;
using UnityEngine.UI;

public class PercentagePreviewTimeUpdater : MonoBehaviour
{
    private Slider attachedSlider;

    private void Awake()
    {
        Chart.ChartFileLoaded += OnEnable;
    }
    
    private void OnEnable()
    {
        attachedSlider = GetComponent<Slider>();
        attachedSlider.onValueChanged.AddListener(UpdatePreview);

        Chart.Metadata.PreviewStartTimeUpdated += UpdatePercentage;
        UpdatePercentage();
    }

    private void OnDisable()
    {
        Chart.Metadata.PreviewStartTimeUpdated -= UpdatePercentage;
    }

    private void UpdatePreview(float newPercent)
    {
        Chart.Metadata.PreviewStartTime = newPercent * SongTime.SongLength;
    }

    private void UpdatePercentage()
    {
        attachedSlider.value = Chart.Metadata.PreviewStartTime / SongTime.SongLength;
    }
}