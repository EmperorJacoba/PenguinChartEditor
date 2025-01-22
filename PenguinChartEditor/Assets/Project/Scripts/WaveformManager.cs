using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveformManager : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;
    LineRenderer lineRenderer;
    // Line renderer uses local positioning to more easily align with the screen and cull points off-screen

    GameObject screenReference; // Panel that is always the size of the screen
    // Use to determine when to stop generating waveform vertices

    InputMap inputMap; // Input System map
    float mouseScrollY;
    
    float shrinkFactor = 0.0001f; // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    // Change shrink factor to modify how tight the waveform looks
    float[] masterWaveformData;

    void Awake() 
    {
        inputMap = new InputMap();

        inputMap.Enable();

        inputMap.Charting.ScrollTrack.performed += x => mouseScrollY = x.ReadValue<float>(); // needs to enable MMB click scroll, but do scrolling of waveform thru this only first
    }

  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        screenReference = GameObject.Find("ScreenReference");
        gameObject.transform.position = screenReference.transform.position + Vector3.back; // Move the waveform in front of the background panel so that it actually appears
        // For whatever reason if this isn't moved back it is always ON the panel at start and doesn't show up

        UpdateWaveformData();
        UpdateWaveformSegment();
    }

    void Update()
    {
        Debug.Log($"Mouse Scroll Y: {mouseScrollY}");
    }

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="audioStem">The BASS stream to get audio samples of.</param>
    public void UpdateWaveformData() // pass in file path here later
    {
        masterWaveformData = pluginBassManager.GetAudioSamples(); // Load in audio samples from PluginBassManager - use param here later
        // Pass in song path to funct in order to update waveform data to display
    }

    void UpdateWaveformSegment()
    {
        // Set data points to display here

        var displayedDataPoints = masterWaveformData.Length; // # of vertices in line depends on # of samples in data array
        lineRenderer.positionCount = displayedDataPoints;

        for (var i = 0; i < displayedDataPoints; i++)
        {
            if (i % 2 == 0) // Since waveform has abs vals, alternate displaying left and right of waveform midline to get centered-esque waveform
            {
                lineRenderer.SetPosition(i, new Vector2(masterWaveformData[i], i*shrinkFactor));
            }
            else
            {
                lineRenderer.SetPosition(i, new Vector2(-masterWaveformData[i], i*shrinkFactor));
            }
        }
        // This needs to be modified to cull points off-screen to enable a more precise waveformDataArray and displayed waveform for proper tempo mapping 
        

        // Get number of points to display based on screen size and shrink factor 
        // Shrink factor is modified by hyperspeed and speed changes
        // Move normalization factor from BASSManager to here to allow for dynamic waveform scaling
        // Get starting point of waveform data based on a waveform position variable modified by scrolling
        // Load in data until displayedDataPoints is full & display
    }
    // Future:
    // Shrink factor needs ability to be changed (hyperspeed)
    // Speed changes need to change shrink factor

}
