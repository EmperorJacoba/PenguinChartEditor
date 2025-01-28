using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class WaveformManager : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;

    LineRenderer lineRenderer;
    // Note: Line renderer uses local positioning to more easily align with the screen and cull points off-screen
    
    RectTransform rt;

    GameObject screenReference; // Panel that is always the size of the screen => used to set waveform object at right distance from camera

    float rtHeight;

    InputMap inputMap;
    
    float shrinkFactor = 0.0001f; // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    // Change shrink factor to modify how tight the waveform looks
    // Shrink factor is modified by hyperspeed and speed changes

    float[] masterWaveformData; 
    int currentWFDataPosition = 0; // Where the user is by *sample count* -> this corresponds to an index in the masterWaveformData array

    readonly int scrollSkip = 100; // How many array indexes to skip when scrolling - this is a "mechanical advantage" for scrolling


    void Awake() 
    {
        inputMap = new();
        inputMap.Enable();

        // inputMap.Charting.ScrollTrack.performed += x => mouseScrollY = x.ReadValue<float>(); // needs to enable MMB click scroll, but do scrolling of waveform thru this only first
        inputMap.Charting.ScrollTrack.performed += scrollChange => UpdateWaveformSegment(scrollChange.ReadValue<float>());
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        rt = gameObject.GetComponent<RectTransform>();
        screenReference = GameObject.Find("ScreenReference");

        gameObject.transform.position = screenReference.transform.position + Vector3.back; // Move the waveform in front of the background panel so that it actually appears\
        // For whatever reason if this isn't moved back it is always ON the panel at start and doesn't show up
        
        rt.pivot = screenReference.GetComponent<RectTransform>().pivot;
        rtHeight = rt.rect.height;

        UpdateWaveformData();
        UpdateWaveformSegment(0);
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

    /// <summary>
    /// Take a value from a mouse scroll and use it to change what waveform data is displayed. 
    /// </summary>
    /// <param name="scrollChange"></param>
    private void UpdateWaveformSegment(float scrollChange)
    {
        // Step 1: Get position of array to start r/w from
        scrollChange *= scrollSkip; // Multiply by value to convert # into a number able to be processed by masterWaveformData array
        // Scroll change can be float from click + drag, int from scroll wheel => must scale up with scrollSkip to get a sort of "mechanical advantage" with scrolling
        Mathf.Round(scrollChange); // Round to int to avoid decimal array positions
        currentWFDataPosition += (int)scrollChange; // Add scrollChange (which is now a # of data points to ffw by) to modify array position

        // Step 2: Check to make sure r/w request is within the bounds of the array
        if (currentWFDataPosition < 0)
        {
            currentWFDataPosition = 0;
            return;
        }
        else if (currentWFDataPosition > masterWaveformData.Length)
        {
            // Edit this so this don't look super weird at the end of the scroll
            currentWFDataPosition = masterWaveformData.Length;
            return;
        }

        // Step 3: Reset line renderer
        lineRenderer.positionCount = 0; // This happens after the bound checks to avoid "flickering" of the drawn array

        // Step 4: Set up start and end points of data to draw
        var displayedDataPoints = currentWFDataPosition + (int)Mathf.Round(rtHeight / shrinkFactor); 
        // ^^ Start from where we are, draw points shrinkFactor distance apart until we hit end of screen (rtHeight)
        lineRenderer.positionCount = displayedDataPoints; // Tell the line renderer that displayedDataPoints is the # of points to draw
        // ^^ Line renderer needs an array initialization in order to draw the correct # of points

        // Step 5: Put points on screen
        float currentYValue = 0;
        for (var i = currentWFDataPosition; i < displayedDataPoints; i++) // i represents index in masterWaveformData, get data from mWD until screen ends
        {
            if (i % 2 == 0) // Since waveform has abs vals, alternate displaying left and right of waveform midline to get centered-esque waveform
            {
                lineRenderer.SetPosition(i, new Vector2(masterWaveformData[i], currentYValue));
            }
            else
            {
                lineRenderer.SetPosition(i, new Vector2(-masterWaveformData[i], currentYValue));
            }
            currentYValue += shrinkFactor; 
            // ^^ Since shrinkFactor represents the y-distance between two drawn waveform points
            // add shrinkFactor to currentYValue to set the next point to be drawn at the next y position
        }

        // Move normalization factor from BASSManager to here to allow for dynamic waveform scaling
    }

        // Next step (of many): Make scrollbar to adjust shrinkFactor and then rerender waveform !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  <= <= <=
        // Just to see if it works, and to check if code is malleable, and to properly code in scrollchange

}

    // Theory: Do not lock tempo events to waveform points - calculating which points are visible and tempo events should be a SEPERATE PROCESS
    // Tempo events & waveform points are essentially different "layers" on the song track
    // # of points = a specific length of song time
    // Use amount of song visible to calculate where a point on the screen would correspond to within the song time
    // Tempo events also in it of themselves calculate the distance between two tempo events based on bpm
    // So 120bpm from tick 0 to tick 192 (0 to first quarter note) is a set distance in seconds & can be shown on the song track based on song time visible
    // Example: 1 second of song is visible, 120bpm = 2 beats per second, so 1 beat = 0.5 seconds
    // Distance from tick 0 and tick 192 (next beat) is half the screen height 

    // To do;
    // Implement click + drag mmb scroll
    // Implement changing of shrink factor
        // Happens via speed & hyperspeed changes

    // Note: for playing audio, generate data like UpdateWaveformData() in chunks above the screen and move down to avoid lag
        // Updating the waveform every frame is unneccesary and will lag the heck out of anyone's machine
    // Maybe edit UpdateWaveformData() to do the same? 
    // Although current usage is runs UWD() way less frequently