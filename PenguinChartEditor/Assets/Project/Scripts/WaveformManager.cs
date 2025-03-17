using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveformManager : MonoBehaviour
{
    PluginBassManager pluginBassManager;
    Strikeline strikeline;

    #region Properties
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

    public delegate void WaveformDisplayDelegate();
    public static event WaveformDisplayDelegate DisplayChanged;

    /// <summary>
    /// The y distance between each waveform point on the line renderer. Default is 0.0001.
    /// <para>Change shrink factor to modify how tight the waveform looks.</para>
    /// <para>Modified by hyperspeed and audio speed changes.</para>
    /// </summary>
    public float ShrinkFactor // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    {
        get
        {
            return _shrinkFactor;
        }
        set
        {
            if (_shrinkFactor == value) return;
            _shrinkFactor = value;
            DisplayChanged?.Invoke();
        }
    }
    private static float _shrinkFactor = 0.001f;

    /// <summary>
    /// The currently displayed waveform.
    /// </summary>
    private static ChartMetadata.StemType CurrentWaveform {get; set;} 

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
    /// Where the user is by sample count at the strikeline.
    /// <para>This corresponds to an index in the WaveformData arrays.</para>
    /// </summary>
    public int CurrentWaveformDataPosition
    {
        get
        {
            return _wfPosition;
        }
        private set
        {
            if (_wfPosition == value) return;
            _wfPosition = value;
        }
    }
    private static int _wfPosition;

    public float Amplitude
    {
        get
        {
            return _amplitude;
        }
        set
        {
            if (_amplitude == value) return;
            _amplitude = value;
            DisplayChanged?.Invoke();
        }
    }
    private static float _amplitude = 3;

    #endregion
    #region Unity Functions
    void Awake() 
    {
        InitializeComponents();
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

        CurrentWaveformDataPosition = 0;
    }

    void Start()
    {
        SetWaveformVisibility(false);
        // Invisible by default so that a bunch of dropdown defaulting logic isn't needed
        // Just have user select it

        SongTimelineManager.TimeChanged += ChangeWaveformSegment;
        DisplayChanged += GenerateWaveformPoints;
        
        rt.pivot = screenReference.GetComponent<RectTransform>().pivot;
        rtHeight = rt.rect.height;

        CurrentWaveform = ChartMetadata.Stems.Keys.First(); // This doesn't matter much b/c waveform is invis by default
        // This is just so that ScrollWaveformSegment has something to generate from

        InitializeWaveformData();
    }
    #endregion

    /// <summary>
    /// Create waveform data for each stem in the ChartMetadata Stems dictionary.
    /// </summary>
    void InitializeWaveformData()
    {
        foreach (var pair in ChartMetadata.Stems)
        {
            UpdateWaveformData(pair.Key);
        }
    }
    
    public void SetWaveformVisibility(bool isVisible)
    {
        // since pathing/playing/etc has been built around the waveform itself (oopsies)
        // you can't disable the line renderer
        // so put it behind everything else and now it's "disabled"
        if (isVisible) transform.position = screenReference.transform.position + Vector3.back;
        // ^^ In order for the waveform to be visible the container game object has to be moved in front of the background panel & vice versa
        else transform.position = screenReference.transform.position - 2*Vector3.back; // 2* b/c this looks weird in the scene view otherwise
    }

    #region Point Generation
    
    /// <summary>
    /// Generate an array of line renderer positions based on waveform audio.
    /// </summary>
    /// <returns>Vector3 array of line renderer positions</returns>
    private void GenerateWaveformPoints()
    {
        GetWaveformProperties(out var masterWaveformData, out var samplesPerScreen, out var strikeSamplePoint);

        lineRendererMain.positionCount = samplesPerScreen;
        lineRendererMirror.positionCount = samplesPerScreen;

        Vector3[] lineRendererPositions = new Vector3[lineRendererMain.positionCount];
        float yPos = 0;

        for (int lineRendererIndex = 0; lineRendererIndex < lineRendererPositions.Length; lineRendererIndex++)
        {
            yPos = lineRendererIndex*ShrinkFactor;
            try
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(masterWaveformData[CurrentWaveformDataPosition + strikeSamplePoint] * Amplitude, yPos);
            }
            catch // this happens when there is no data to pull for the waveform
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(0, yPos);
                // this way the beginning and end of the waveform will stop at the strikeline instead of screen boundaries
            }
            strikeSamplePoint++; // this allows working with the waveform data from the bottom up & for CurrentWFDataPosition to be at the strikeline
        }
        
        lineRendererMain.SetPositions(lineRendererPositions);

        // mirror all x positions of every point
        lineRendererPositions = Array.ConvertAll(lineRendererPositions, pos => new Vector3(-pos.x, pos.y));

        lineRendererMirror.SetPositions(lineRendererPositions);
    }
    #endregion

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="stem">The BASS stream to get audio samples of.</param>
    public void UpdateWaveformData(ChartMetadata.StemType stem) // pass in file path here later
    {
        float[] stemWaveformData = pluginBassManager.GetAudioSamples(stem, out long bytesPerSample); 

        if (WaveformData.ContainsKey(stem))
        {
            WaveformData.Remove(stem);
        } // Flush current value to allow for new one

        WaveformData.Add(stem, (stemWaveformData, bytesPerSample));
    }

    /// <summary>
    /// Calculate necessary data to generate/access waveform points.
    /// </summary>
    /// <param name="masterWaveformData">Current array of waveform data to pull from.</param>
    /// <param name="samplesPerScreen">The number of sample points that can be displayed on screen, based on the current shrinkFactor.</param>
    /// <param name="strikeSamplePoint">The number of sample points displayed from the bottom of the screen to the strikeline. THIS VALUE IS NEGATIVE BY DEFAULT</param>
    public void GetWaveformProperties(out float[] masterWaveformData, out int samplesPerScreen, out int strikeSamplePoint)
    {
        masterWaveformData = WaveformData[CurrentWaveform].Item1;
        samplesPerScreen = (int)Mathf.Round(rtHeight / ShrinkFactor);
        strikeSamplePoint = (int)Math.Ceiling(-samplesPerScreen * strikeline.GetStrikelineScreenProportion()); // note the negative sign
    }

    public void ChangeWaveformSegment()
    {
        // This can use an implicit cast because song position is always rounded to 3 decimal places
        CurrentWaveformDataPosition = (int)(SongTimelineManager.SongPosition * PluginBassManager.SAMPLES_PER_SECOND);

        GenerateWaveformPoints();
    }

    /// <summary>
    /// Update the visible and calculated-upon waveform.
    /// </summary>
    /// <param name="stem">The stem to set to the active waveform.</param>
    public void ChangeDisplayedWaveform(ChartMetadata.StemType stem)
    {
        SetWaveformVisibility(true);
        CurrentWaveform = stem;
        GenerateWaveformPoints();
    }

    /// <summary>
    /// Get the start and end second values of the visible waveform segment.
    /// </summary>
    /// <param name="startPoint">The first waveform point visible, in seconds.</param>
    /// <param name="endPoint">The last waveform point visible, in seconds</param>
    public (double, double) GetDisplayedAudio()
    {
        GetWaveformProperties(out var _, out var samplesPerScreen, out var strikeSamplePoint);

        // get to bottom of screen, calculate what that waveform position is in seconds
        var startPoint = (CurrentWaveformDataPosition + strikeSamplePoint) * PluginBassManager.ARRAY_RESOLUTION; // change pls
        // get to bottom of screen, jump to top of screen with samplesPerScreen, calculate what that waveform position is in seconds
        var endPoint = (CurrentWaveformDataPosition + strikeSamplePoint + samplesPerScreen) * PluginBassManager.ARRAY_RESOLUTION; // change pls

        return (startPoint, endPoint);
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
    // Implement calibration
    // then, on to beatlines...

    // Let CurrentWFDataPosition scroll to length of longest stem