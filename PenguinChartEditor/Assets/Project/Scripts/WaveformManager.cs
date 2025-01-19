using UnityEngine;

public class WaveformManager : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;
    LineRenderer lineRenderer;
  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        var waveformDataArray = pluginBassManager.GetAudioSamples(); // Load in audio samples from PluginBassManager
        var displayedDataPoints = waveformDataArray.Length; // # of vertices in line depends on # of samples in data array
        lineRenderer.positionCount = displayedDataPoints;

        var shrinkFactor = 0.001f; // Needed to compress the points into something legible (y value * shrinkFactor = y position)
        // Change shrink factor to modify how tight the waveform looks

        for (var i = 0; i < displayedDataPoints; i++)
        {
            if (i % 2 == 0) // Since waveform has abs vals, alternate displaying left and right of waveform midline
            {
                lineRenderer.SetPosition(i, new Vector2(waveformDataArray[i], i*shrinkFactor));
            }
            else
            {
                lineRenderer.SetPosition(i, new Vector2(-waveformDataArray[i], i*shrinkFactor));
            }
        }
        // This needs to be modified to cull points off-screen to enable a more precise waveformDataArray and displayed waveform for proper tempo mapping 
    }
}
