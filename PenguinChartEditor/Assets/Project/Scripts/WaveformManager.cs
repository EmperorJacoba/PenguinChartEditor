using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Un4seen.Bass;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveformManager : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;

    LineRenderer lineRenderer;
    // Note: Line renderer uses local positioning to more easily align with the screen and cull points off-screen
    
    RectTransform rt;

    GameObject screenReference; // Panel that is always the size of the screen => used to set waveform object at right distance from camera

    float rtHeight;

    private InputMap inputMap;
    
    readonly float defaultShrinkFactor = 0.0001f;
    float shrinkFactor = 0.0001f; // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    // Change shrink factor to modify how tight the waveform looks
    // Shrink factor is modified by hyperspeed and speed changes

    ChartMetadata.StemType currentWaveform;  
    public static Dictionary<ChartMetadata.StemType, (float[], long)> waveformData = new();
    // This dictionary holds all the different waveform data in case different stems want to be shown on the waveform
    // ChartMetadata.StemType is the identifier for which audio stem the data belongs to
    // The tuple in the value holds the data (float[]) and the number of bytes per sample (long)
    // The number of bytes per sample is needed in order to accurately play and seek through the track in PluginBassManager
    // The number of bytes can vary based on the type of audio file the user inputs, like if they use .opus, .mp3 together, etc.
    // long is just what Bass returns and I don't want to do a million casts just to make this a regular int
    // 64 bit values are actually kinda baller in my opinion so i'm not opposed 

    public static int currentWFDataPosition = 0; // Where the user is by *sample count* -> this corresponds to an index in waveformData arrays

    readonly int scrollSkip = 100; // How many array indexes to skip when scrolling - this is a "mechanical advantage" for scrolling

    int chunkSamples = 2000;

    // Needed for delta calculations when scrolling using MMB
    private float initialMouseY = float.NaN;
    private float currentMouseY;

    void Awake() 
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ScrollTrack.performed += scrollChange => ScrollWaveformSegment(scrollChange.ReadValue<float>(), false);

        inputMap.Charting.MiddleScrollMousePos.performed += x => currentMouseY = x.ReadValue<Vector2>().y;
        inputMap.Charting.MiddleScrollMousePos.Disable(); // This is disabled immediately so that it's not running when it's not needed

        inputMap.Charting.MiddleMouseClick.started += x => ChangeMiddleClick(true);
        inputMap.Charting.MiddleMouseClick.canceled += x => ChangeMiddleClick(false);
        
        // Put all this input map stuff in a seperate file later on
    }

    public void ToggleCharting()
    {
        if (inputMap.Charting.enabled)
        {
            inputMap.Charting.Disable();
        }
        else
        {
            inputMap.Charting.Enable();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        rt = gameObject.GetComponent<RectTransform>();
        screenReference = GameObject.Find("ScreenReference");

        gameObject.transform.position = screenReference.transform.position + Vector3.back; 
        // ^^ Move the waveform in front of the background panel so that it actually appears\
        // For whatever reason if this isn't moved back it is always ON the panel at start and doesn't show up
        
        rt.pivot = screenReference.GetComponent<RectTransform>().pivot;
        rtHeight = rt.rect.height;

        currentWaveform = ChartMetadata.StemType.song; // testing
        UpdateWaveformData(ChartMetadata.StemType.song);
        ScrollWaveformSegment(0, false);
    }

    double audioPosition = -1;
    double lastAudioPosition = -1;
    void Update()
    {
        if (inputMap.Charting.MiddleScrollMousePos.enabled)
        {
            ScrollWaveformSegment(currentMouseY - initialMouseY, true);
            // This runs every frame to get that smooth scrolling effect like on webpages and such
            // If this ran when the mouse was moved then it would be super jumpy
        }

        if (pluginBassManager.audioPlaying)
        {
            audioPosition = Bass.BASS_ChannelBytes2Seconds(pluginBassManager.stemStreams[ChartMetadata.StemType.song], Bass.BASS_ChannelGetPosition(pluginBassManager.stemStreams[ChartMetadata.StemType.song])); 
            if (lastAudioPosition == -1)
            {
                lastAudioPosition = audioPosition;
            }
            var localYChange = ((float)(audioPosition - lastAudioPosition) / pluginBassManager.compressedArrayResolution) * shrinkFactor;

            Vector3[] test = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(test);
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                lineRenderer.SetPosition(i, test[i] - new Vector3(0, localYChange, 0));
            }

            lastAudioPosition = audioPosition;
        }
    }

    void ChangeMiddleClick(bool clickStatus)
    {
        if (clickStatus)
        {
            inputMap.Charting.MiddleScrollMousePos.Enable(); // Allow calculations of middle mouse scroll now that MMB is clicked

            initialMouseY = Input.mousePosition.y;
            currentMouseY = Input.mousePosition.y;
            // ^^ These HAVE to be here. I really didn't want to use the old input system for this (for unity in the literal sense)
            // but initial & current mouse positions need to be initialized right this instant in order to get a 
            // proper delta calculation in Update().
            // Without this the waveform jumps to the beginning when you click MMB without moving
        }
        else
        {
            inputMap.Charting.MiddleScrollMousePos.Disable();
            initialMouseY = float.NaN; 
            // Kind of a relic from testing, but I'm keeping this here because I feel like this is somewhat helpful in case this is improperly used somewhere
        }
    }

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="audioStem">The BASS stream to get audio samples of.</param>
    public void UpdateWaveformData(ChartMetadata.StemType stem) // pass in file path here later
    {
        float[] stemWaveformData = pluginBassManager.GetAudioSamples("", out long bytesPerSample); 
        stemWaveformData = Normalize(stemWaveformData, 5); // Modify obtained data to reduce peaks

        if (waveformData.ContainsKey(stem))
        {
            waveformData.Remove(stem);
        } // Flush current value to allow for new one

        waveformData.Add(stem, (stemWaveformData, bytesPerSample));
    }

    // This is here so that waveform peak lengths can be changed by user later on
    float[] Normalize(float[] samples, float divideBy)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] /= divideBy; // Change sample value b/c line render uses local positioning and w/o this the peaks are too big
            // If original values are used, waveform looks crazy by default
        }
        return samples;
    }

    /// <summary>
    /// Take a value from a mouse scroll wheel delta and use it to change what waveform data is displayed. 
    /// </summary>
    /// <param name="scrollChange"> Input from scroll method used to move visible parts of waveform </param>
    /// <param name="isMiddleScroll"/> Used to correctly scroll with the middle mouse button </param>
    public void ScrollWaveformSegment(float scrollChange, bool isMiddleScroll)
    {
        // Change scrolling so that it scales with shrink factor (not always so large comparative to the # of samples on screen)

        // Step 1: Get data and sample calculations
        SetUpWaveformChange(out var masterWaveformData, out var samplesPerScreen);

        // Scroll change can be float from click + drag, int from scroll wheel => int must scale up with scrollSkip to get a sort of "mechanical advantage" with scrolling
        // Step 2: Get position of array to start r/w from
        if (!isMiddleScroll) // Cursor delta is based on pixels so this upscaling isn't really needed for a non-MMB scroll
        {
            scrollChange *= scrollSkip; // Multiply by value to convert # into a number able to be processed by masterWaveformData array
            // Scroll from scroll wheel yields super small values
        }

        scrollChange = Mathf.Round(scrollChange); // Round to int to avoid decimal array positions
        currentWFDataPosition += (int)scrollChange; // Add scrollChange (which is now a # of data points to ffw by) to modify array position

        // Step 3: Check to make sure r/w request is within the bounds of the array
        // returns because no need to rerender lol
        if (currentWFDataPosition < 0)
        {
            currentWFDataPosition = 0;
            return;
        }
        else if (currentWFDataPosition + samplesPerScreen > masterWaveformData.Length)
        {
            currentWFDataPosition = masterWaveformData.Length - samplesPerScreen; 
            // position is from bottom of screen so position should never allow for an index that promises more samples than available
            return;
        }

        // Step 4: Reset Line Renderer (this seems to be redundant at the moment, need to fix so that it scales with screen size)
        // Tell the line renderer that it will need to draw the amount of points that will fit on screen with current settings
        lineRenderer.positionCount = samplesPerScreen;
        // Step 5: Generate points
        GenerateWaveformPoints(masterWaveformData, currentWFDataPosition + samplesPerScreen);
    }

    private void SetUpWaveformChange(out float[] masterWaveformData, out int samplesPerScreen)
    {
        masterWaveformData = waveformData[currentWaveform].Item1;
        samplesPerScreen = (int)Mathf.Round(rtHeight / shrinkFactor);
    }

    private void GenerateWaveformPoints(float[] masterWaveformData, int dataPointsToDisplay)
    {
        // ^^ Line renderer needs an array initialization in order to draw the correct # of points
        float currentYValue = 0;
        int lineRendererIndex = 0; // This must be seperate because line renderer index is NOT the same as either i comparison variable
        // theoretically you could do some subtraction to figure out the index but this is just simpler & easier
        for (var i = currentWFDataPosition; i < dataPointsToDisplay; i++) // i represents index in masterWaveformData, get data from mWD until screen ends
        {
            if (i % 2 == 0) // Since waveform has abs vals, alternate displaying left and right of waveform midline to get centered-esque waveform
            {
                lineRenderer.SetPosition(lineRendererIndex, new Vector2(masterWaveformData[i], currentYValue));
            }
            else
            {
                lineRenderer.SetPosition(lineRendererIndex, new Vector2(-masterWaveformData[i], currentYValue));
            }
            lineRendererIndex++;
            currentYValue += shrinkFactor; 
            // ^^ Since shrinkFactor represents the y-distance between two drawn waveform points
            // add shrinkFactor to currentYValue to set the next point to be drawn at the next y position
        }

    }

    /* public void ChunkWaveformSegment()
    {
        SetUpWaveformChange(out var masterWaveformData, out var samplesPerScreen);

        lineRenderer.positionCount = samplesPerScreen + chunkSamples;

        GenerateWaveformPoints(masterWaveformData, currentWFDataPosition + samplesPerScreen + chunkSamples);
    } */


    // Just testing for now
    public void ChangeShrinkFactor(float scrollbarPosition)
    {
        shrinkFactor = defaultShrinkFactor + 0.0001f*scrollbarPosition*20;
        ScrollWaveformSegment(0, false);
    }
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