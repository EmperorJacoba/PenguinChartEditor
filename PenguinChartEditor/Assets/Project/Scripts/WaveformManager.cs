using System;
using System.Collections.Generic;
using Un4seen.Bass;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveformManager : MonoBehaviour
{
    PluginBassManager pluginBassManager;
    Strikeline strikeline;

    /// <summary>
    /// Line renderer that contains rightward (positive dir) waveform render
    /// </summary>
    LineRenderer lineRendererMain;

    /// <summary>
    /// Line renderer that contains leftward (negative dir) waveform render
    /// </summary>
    LineRenderer lineRendererMirror;

    // Note: Line renderer uses local positioning to more easily align with the screen and cull points off-screen
    // both of these line renderers combine to make a symmetrical waveform
    // and the center is hollow! so cool and unique
    
    /// <summary>
    /// RectTransform attached to the waveform container.
    /// </summary>
    RectTransform rt;

    /// <summary>
    /// Height of the RectTransform component attached to the waveform's container GameObject.
    /// </summary>
    private float rtHeight;

    /// <summary>
    /// Panel that is always the size of the screen. Used to set waveform object at right distance from camera.
    /// </summary>
    GameObject screenReference; 

    private InputMap inputMap;
    
    /// <summary>
    /// The y distance between each waveform point on the line renderer. Default is 0.0001.
    /// <para>Change shrink factor to modify how tight the waveform looks.</para>
    /// <para>Modified by hyperspeed and audio speed changes.</para>
    /// </summary>
    public static float ShrinkFactor {get; set;} // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    private static readonly float defaultShrinkFactor = 0.0001f;

    /// <summary>
    /// The currently displayed waveform.
    /// </summary>
    public ChartMetadata.StemType CurrentWaveform {get; set;} 

    /// <summary>
    /// Dictionary that contains waveform point data for each song stem.
    /// <para>ChartMetadata.StemType is the audio stem the data belongs to</para>
    /// <para>The tuple in the value holds the data (float[]) and the number of bytes per sample (long)</para>
    /// </summary>
    public static Dictionary<ChartMetadata.StemType, (float[], long)> WaveformData {get; private set;}
    // The number of bytes per sample is needed in order to accurately play and seek through the track in PluginBassManager
    // The number of bytes can vary based on the type of audio file the user inputs, like if they use .opus, .mp3 together, etc.
    // long is just what Bass returns and I don't want to do a million casts just to make this a regular int
    // 64 bit values are actually kinda baller in my opinion so i'm not opposed 

    /// <summary>
    /// Where the user is by sample count.
    /// <para>This corresponds to an index in the WaveformData arrays.</para>
    /// </summary>
    public static int CurrentWFDataPosition {get; set;}

    /// <summary>
    /// How many array indexes to skip when scrolling with wheel
    /// <para>This works like mechanical advantage so that scrolling the waveform isn't super slow.</para>
    /// </summary>
    private readonly int scrollSkip = 100; 

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

    private void InitializeComponents()
    {
        WaveformData = new();

        lineRendererMain = GetComponent<LineRenderer>();
        lineRendererMirror = transform.GetChild(0).gameObject.GetComponent<LineRenderer>();

        rt = gameObject.GetComponent<RectTransform>();

        pluginBassManager = GameObject.Find("PluginBassManager").GetComponent<PluginBassManager>();
        screenReference = GameObject.Find("ScreenReference");
        strikeline = GameObject.Find("Strikeline").GetComponent<Strikeline>();
        ShrinkFactor = 0.0001f;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeComponents();
        
        gameObject.transform.position = screenReference.transform.position + Vector3.back; 
        // ^^ Move the waveform in front of the background panel so that it actually appears
        // For whatever reason if this isn't moved back it is always ON the panel at start and doesn't show up
        
        rt.pivot = screenReference.GetComponent<RectTransform>().pivot;
        rtHeight = rt.rect.height;

        CurrentWaveform = ChartMetadata.StemType.song; // testing
        CurrentWFDataPosition = 0;
        UpdateWaveformData(ChartMetadata.StemType.song);
        ScrollWaveformSegment(0, false);
        // Ratio of bottom to strikeline and strikeline to top should be the same between waveform line renderer and strikeline line renderer
        // Pivot & Center of game object is at bottom of screen
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
            // a time delta is needed to move the waveform properly and this is the most reliable way to do it afaik
            audioPosition = Bass.BASS_ChannelBytes2Seconds(pluginBassManager.stemStreams[ChartMetadata.StemType.song], Bass.BASS_ChannelGetPosition(pluginBassManager.stemStreams[ChartMetadata.StemType.song])); 
            
            // ask me about this (localYChange) and i will rant at you about the NONSENSE that requires this
            // unity coordinate systems are just...not consistent? between world, local, and line renderer spaces
            // for some odd reason if you want to move them all down at once by moving the container there is ZERO way to convert between
            // a line renderer/local coordinate and world space coordinate shift...or maybe there is and i'm just blind?
            // Transform.TransformPoint did not work!
            // this was just what worked the easiest, sigh - here as a warning to future me, 
            // don't try to work the chunking system because this is miles simpler

            // anyways this is just how much to subtract from the y-pos of each line renderer point each frame to move at the pace of the audio
            // since y distance between points is based on a (time) value, the audio delta 
            // divided by the y-distance between two points (the array res and the shrinkFactor) yields the corresponding distance delta to an audio delta 
            var localYChange = (float)(audioPosition - lastAudioPosition) / pluginBassManager.compressedArrayResolution * ShrinkFactor;
            
            // this picks the good points out of the current array of points 
            // and discards the old ones that fall below the screen
            Vector3[] currentPositions = new Vector3[lineRendererMain.positionCount]; // # of points is still the same by the time this is done
            lineRendererMain.GetPositions(currentPositions);

            var modifiedPositions = TransformLineRendererPoints(currentPositions, localYChange, out var modifiedArrayStopPoint);

            DisplayWaveformPoints(GenerateWaveformPoints(modifiedPositions, modifiedArrayStopPoint));
            lastAudioPosition = audioPosition; // set up stuff for next loop
        }
    }

    /// <summary>
    /// Takes an array of line renderer positions and transforms them down by a specified Y change, while also culling points that fall below the screen.
    /// </summary>
    /// <param name="currentPositions">The array of positions to transform.</param>
    /// <param name="yChange">The amount to transform by.</param>
    /// <param name="modifiedArrayStopPoint">The last filled position of the transformed array of positions.</param>
    /// <returns>Vector3[] array of transformed points, with extra empty positions for each culled point</returns>
    private Vector3[] TransformLineRendererPoints(Vector3[] currentPositions, float yChange, out int modifiedArrayStopPoint)
    {
        Vector3[] modifiedPositions = new Vector3[lineRendererMain.positionCount];
        int modifiedArrayPosition = 0;

        for (int i = 0; i < currentPositions.Length; i++)
        {
            var vectorChange = currentPositions[i] - new Vector3(0, yChange, 0);
            if (vectorChange.y < 0) // weed out anything below bottom of screen -> pivot is at bottom of screen
            {
                CurrentWFDataPosition++; // advance position for each culled point
            }
            else // apply change for valid values
            {
                modifiedPositions[modifiedArrayPosition] = vectorChange;
                modifiedArrayPosition++;
            }
        }
        modifiedArrayStopPoint = modifiedArrayPosition;
        return modifiedPositions;
    }

    /// <summary>
    /// Set values used to calculate audio deltas while playing to -1 for use the next time the audio is played.
    /// </summary>
    public void ResetAudioPositions()
    {
        audioPosition = 0;
        lastAudioPosition = 0;
    }

    /// <summary>
    /// Generate waveform points based on a moving waveform (e.g when it is playing)
    /// </summary>
    /// <param name="modifiedPositions">Remaining points from off-screen cull (existing points with Y change applied).</param>
    /// <param name="modifiedArrayStopPoint">The first empty, ungenerated Vector3 position in the array of points.</param>
    private Vector3[] GenerateWaveformPoints(Vector3[] modifiedPositions, int modifiedArrayStopPoint)
    {
        SetUpWaveformChange(out var masterWaveformData, out var samplesPerScreen, out var strikeSamplePoint);

        var startingY = modifiedPositions[modifiedArrayStopPoint - 1].y + ShrinkFactor;
        // ^^ start drawing more points where the last valid position of the point migration left off 
        // ^^ if you don't start with a shrinkFactor points will crush themselves (gets smaller every loop)
        // ^^ I don't understand why, but if you try to do modifiedArrayStopPoint - 1 in any other spot BESIDES here the playing will not work properly
        // ^^ I tried adding it to TransformLineRendererPoints to make things simpler but that has no bearing on stuff that happens here???????
        // ^^ It makes zero sense but it works this way so I don't feel like touching it to figure out why.

        // This is how many points were culled from the original array of positions
        var pointChange = modifiedPositions.Length - modifiedArrayStopPoint;

        // This is the index to START pulling data points from for the end of the line renderer array
        var pullPoint = CurrentWFDataPosition - pointChange + strikeSamplePoint + samplesPerScreen;
        // Take current strikeline position (CurrentWFDataPosition), subtract how many points were culled to get the past frame's strikeline alignment
        // Then get to the bottom of the screen by adding strikeSamplePoint (which is negative)
        // Then get to last frame's top of screen with samplesPerScreen (which is where new data for the current frame will begin)
        // Then iterate from this point like generating any other section of the waveform

        for (int i = modifiedArrayStopPoint; i < modifiedPositions.Length; i++)
        {
            try
            {
                modifiedPositions[i] = new Vector3(masterWaveformData[pullPoint], startingY);
            }
            catch // this happens when there is no data to pull for the waveform
            {
                modifiedPositions[i] = new Vector3(0, startingY); // so make it null
                // this way the beginning and end of the waveform will stop at the strikeline instead of screen boundaries
            }
            startingY += ShrinkFactor;
            pullPoint++;
        }
        return modifiedPositions;
    }
    
    /// <summary>
    /// Generate an array of line renderer positions based on waveform audio.
    /// </summary>
    /// <returns>Vector3 array of line renderer positions</returns>
    private Vector3[] GenerateWaveformPoints()
    {
        SetUpWaveformChange(out var masterWaveformData, out var samplesPerScreen, out var strikeSamplePoint);

        Vector3[] lineRendererPositions = new Vector3[lineRendererMain.positionCount];
        float yPos = 0;

        for (int lineRendererIndex = 0; lineRendererIndex < lineRendererPositions.Length; lineRendererIndex++)
        {
            try
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(masterWaveformData[CurrentWFDataPosition + strikeSamplePoint], yPos);
            }
            catch // this happens when there is no data to pull for the waveform
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(0, yPos);
                // this way the beginning and end of the waveform will stop at the strikeline instead of screen boundaries
            }
            yPos += ShrinkFactor;
            strikeSamplePoint++; // this allows working with the waveform data from the bottom up & for CurrentWFDataPosition to be at the strikeline
        }
        return lineRendererPositions;
    }


    /// <summary>
    /// Enable/disable middle click movement upon press/release
    /// </summary>
    /// <param name="clickStatus">true = MMB pressed, false = MMB released</param>
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
    /// <param name="stem">The BASS stream to get audio samples of.</param>
    public void UpdateWaveformData(ChartMetadata.StemType stem) // pass in file path here later
    {
        float[] stemWaveformData = pluginBassManager.GetAudioSamples("", out long bytesPerSample); 
        stemWaveformData = Normalize(stemWaveformData, 5); // Modify obtained data to reduce peaks

        if (WaveformData.ContainsKey(stem))
        {
            WaveformData.Remove(stem);
        } // Flush current value to allow for new one

        WaveformData.Add(stem, (stemWaveformData, bytesPerSample));
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
    /// Calculate necessary data to generate waveform points.
    /// </summary>
    /// <param name="masterWaveformData">Current array of waveform data to pull from.</param>
    /// <param name="samplesPerScreen">The number of sample points that can be displayed on screen, based on the current shrinkFactor.</param>
    /// <param name="strikeSamplePoint">The number of sample points displayed from the bottom of the screen to the strikline. THIS VALUE IS NEGATIVE BY DEFAULT</param>
    private void SetUpWaveformChange(out float[] masterWaveformData, out int samplesPerScreen, out int strikeSamplePoint)
    {
        masterWaveformData = WaveformData[CurrentWaveform].Item1;
        samplesPerScreen = (int)Mathf.Round(rtHeight / ShrinkFactor);
        strikeSamplePoint = (int)Math.Ceiling(-samplesPerScreen * strikeline.CalculateStrikelineScreenProportion()); // note the negative sign
    }

    /// <summary>
    /// Take a value from a mouse scroll wheel delta and use it to change what waveform data is displayed. 
    /// <para>Also used to initialize waveform display (scrollChange = 0)</para>
    /// </summary>
    /// <param name="scrollChange">Input from scroll method used to move visible parts of waveform</param>
    /// <param name="isMiddleScroll">Used to correctly scroll with the middle mouse button</param>
    public void ScrollWaveformSegment(float scrollChange, bool isMiddleScroll)
    {
        // Get base calculations before starting anything (strikeSamplePoint not needed here fyi)
        SetUpWaveformChange(out var masterWaveformData, out var samplesPerScreen, out var strikeSamplePoint);

        // Scroll change can be float from click + drag, int from scroll wheel => int must scale up with scrollSkip to get a sort of "mechanical advantage" with scrolling
        // Get position of array to start r/w from
        if (!isMiddleScroll) // Cursor delta is based on pixels so this upscaling isn't really needed for a non-MMB scroll
        {
            scrollChange *= scrollSkip; // Multiply by value to convert the scroll into a number able to be processed by masterWaveformData array
            // Scroll from scroll wheel yields super small values
        }

        scrollChange = Mathf.Round(scrollChange); // Round to int to avoid decimal array positions
        CurrentWFDataPosition += (int)scrollChange; // Add scrollChange (which is now a # of data points to ffw by) to modify array position

        // Check to make sure r/w request is within the bounds of the array
        if (CurrentWFDataPosition < 0)
        {
            CurrentWFDataPosition = 0;
        }
        else if (CurrentWFDataPosition > masterWaveformData.Length)
        {
            CurrentWFDataPosition = masterWaveformData.Length;
            // position is from strikeline so so long as the position is never outside of the array then we chilling
            // try/catch takes care of making sure it doesn't try displaying nonreal points
        }

        // Tell the line renderers that they will need to draw the amount of points that will fit on screen with current settings
        lineRendererMain.positionCount = samplesPerScreen;
        lineRendererMirror.positionCount = samplesPerScreen;

        // Generate points with new position
        DisplayWaveformPoints(GenerateWaveformPoints());
    }

    /// <summary>
    /// Take an array of line renderer points and display them as a symmetrical waveform on a 2D plane.
    /// </summary>
    /// <param name="positions">The array of line renderer positions to display</param>
    private void DisplayWaveformPoints(Vector3[] positions)
    {
        lineRendererMain.SetPositions(positions);

        // use LINQ to mirror all x positions of every point
        positions = Array.ConvertAll(positions, pos => new Vector3(-pos.x, pos.y));

        lineRendererMirror.SetPositions(positions);
    }

    // Just testing for now
    public void ChangeShrinkFactor(float scrollbarPosition)
    {
        ShrinkFactor = defaultShrinkFactor + 0.0001f*scrollbarPosition*20;
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
    // Implement changing of shrink factor
        // Happens via speed & hyperspeed changes



    // CURRENT TO DO:
    // move strikeline up
        // make it moveable?
        // let current waveform pos go below screen so that first peak can be seen at the strikeline
        // also let current waveform pos go a bit above screen so that it can stop at the strikeline instead of bottom of screen too
        // maybe put a marker where waveform stops?
    // waveform data position does not update correctly when audio playing stops
    // ORRR just grab all the points, mirror them, and put them in another line renderer on top of the existing one
    // then, on to beatlines...