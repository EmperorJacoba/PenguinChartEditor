using System;
using System.Collections.Generic;
using Un4seen.Bass;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveformManager : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;

    LineRenderer lineRendererMain;
    LineRenderer lineRendererMirror;
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
        lineRendererMain = GetComponent<LineRenderer>();
        lineRendererMirror = transform.GetChild(0).gameObject.GetComponent<LineRenderer>();
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
            // This if block (which should be refactored later) plays the waveform in sync with the audio
            // "strikeline" is currently at bottom of screen

            // Step: Get audio delta
            // Time.deltaTime or a coroutine don't work properly for some reason 
            // (probably due to difference between song timing & frame timing idk)
            // a time delta is needed to move the waveform and this is the most reliable way to do it afaik
            audioPosition = Bass.BASS_ChannelBytes2Seconds(pluginBassManager.stemStreams[ChartMetadata.StemType.song], Bass.BASS_ChannelGetPosition(pluginBassManager.stemStreams[ChartMetadata.StemType.song])); 
            if (lastAudioPosition == -1)
            {
                lastAudioPosition = audioPosition; // avoids some funky math and also holdovers from last time audio is played
            }
            
            // ask me about this (localYChange) and i will rant at you about the NONSENSE that requires this
            // unity coordinate systems are just...not consistent? between world, local, and line renderer spaces
            // for some odd reason if you want to move them all down at once by moving the container there is ZERO way to convert between
            // a line renderer/local coordinate and world space coordinate shift...or maybe there is and i'm just blind?
            // this was just what worked the easiest, sigh - here as a warning to future me, 
            // don't try to work the chunking system because this is miles simpler

            // anyways this is just how much to subtract from the y-pos of each line renderer point each frame to move at the pace of the audio
            // since y distance between points is based on a (time) value, the audio delta 
            // divided by the y-distance between two points yields the corresponding distance delta to an audio delta 
            var localYChange = (float)(audioPosition - lastAudioPosition) / pluginBassManager.compressedArrayResolution * shrinkFactor;
            
            // this picks the good points out of the current array of points 
            // and discards the old ones that fall below the screen
            Vector3[] currentPositions = new Vector3[lineRendererMain.positionCount];
            Vector3[] modifiedPositions = new Vector3[lineRendererMain.positionCount]; // # of points is stil the same by the time this is done
            lineRendererMain.GetPositions(currentPositions);

            int leftOutPoints = 0; 
            int modifiedArrayStopPoint = 0; // add these two vals up and you get the length of the array btw

            for (int i = 0; i < currentPositions.Length; i++)
            {
                var vectorChange = currentPositions[i] - new Vector3(0, localYChange, 0);
                if (vectorChange.y < 0) // weed out anything below bottom of screen -> pivot is at bottom of screen
                {
                    leftOutPoints++;
                }
                else
                {
                    modifiedPositions[modifiedArrayStopPoint] = vectorChange;
                    modifiedArrayStopPoint++;
                }
            }

            currentWFDataPosition += leftOutPoints; // advance array by # of points destroyed
            float startingY = modifiedPositions[modifiedArrayStopPoint - 1].y + shrinkFactor; 
            // ^^ start drawing more points where the last valid position of the point migration left off 
            // ^^ if you don't start with a shrinkFactor points will crush themselves (gets smaller every loop)
            float[] masterWaveformData = waveformData[currentWaveform].Item1;

            // add on extra points to replace the ones that fell below the screen
            for (int i = 0; i < leftOutPoints; i++)
            {
                var pullPoint = modifiedArrayStopPoint + i;
                try
                {
                    modifiedPositions[pullPoint] = new Vector3(masterWaveformData[currentWFDataPosition + pullPoint], startingY);
                    startingY += shrinkFactor;
                }
                catch // this is if there is no more data to pull but the waveform is still physically playing
                {
                    modifiedPositions[pullPoint] = new Vector3(0, startingY);
                }
            }
            MirrorWavePoints(modifiedPositions); // cave johnson, we're done here, throw it on the screen
            lastAudioPosition = audioPosition; // set up stuff for next loop
        }
    }

    public void ResetAudioPositions()
    {
        audioPosition = -1;
        lastAudioPosition = -1;
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
        if (currentWFDataPosition < 0)
        {
            currentWFDataPosition = 0;
        }
        else if (currentWFDataPosition + samplesPerScreen > masterWaveformData.Length)
        {
            currentWFDataPosition = masterWaveformData.Length - samplesPerScreen; 
            // position is from bottom of screen so position should never allow for an index that promises more samples than available
        }

        // Step 4: Reset Line Renderer (this seems to be redundant at the moment, need to fix so that it scales with screen size)
        // Tell the line renderer that it will need to draw the amount of points that will fit on screen with current settings
        lineRendererMain.positionCount = samplesPerScreen;
        lineRendererMirror.positionCount = samplesPerScreen;
        // Step 5: Generate points
        GenerateWaveformPoints(masterWaveformData, samplesPerScreen);
    }

    private void SetUpWaveformChange(out float[] masterWaveformData, out int samplesPerScreen)
    {
        masterWaveformData = waveformData[currentWaveform].Item1;
        samplesPerScreen = (int)Mathf.Round(rtHeight / shrinkFactor);
    }

    private void GenerateWaveformPoints(float[] masterWaveformData, int samplesPerScreen)
    {
        float currentYValue = 0;
        int lineRendererIndex = 0; // This must be seperate because line renderer index is NOT the same as either i comparison variable
        // theoretically you could do some subtraction to figure out the index but this is just simpler & easier
        Vector3[] lineRendererPositions = new Vector3[lineRendererMain.positionCount];
        
        for (var i = currentWFDataPosition; i < currentWFDataPosition + samplesPerScreen; i++)
        {
            lineRendererPositions[lineRendererIndex] = new Vector3(masterWaveformData[i], currentYValue);
            lineRendererIndex++;
            currentYValue += shrinkFactor; 
            // ^^ Since shrinkFactor represents the y-distance between two drawn waveform points
            // add shrinkFactor to currentYValue to set the next point to be drawn at the next y position
        }
        MirrorWavePoints(lineRendererPositions);
    }

    private void MirrorWavePoints(Vector3[] positions)
    {
        lineRendererMain.SetPositions(positions);
        positions = Array.ConvertAll(positions, x => new Vector3(-x.x, x.y));
        lineRendererMirror.SetPositions(positions);
    }

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



    // CURRENT TO DO:
    // move strikeline up
        // make it moveable?
        // let current waveform pos go below screen so that first peak can be seen at the strikeline
        // also let current waveform pos go a bit above screen so that it can stop at the strikeline instead of bottom of screen too
        // maybe put a marker where waveform stops?
    // waveform data position does not update correctly when audio playing stops
    // ORRR just grab all the points, mirror them, and put them in another line renderer on top of the existing one
    // then, on to beatlines & rendering that shit...