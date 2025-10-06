using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Waveform : MonoBehaviour
{
    static Waveform instance;

    /// <summary>
    /// Dictionary that contains waveform point data for each song stem.
    /// <para>ChartMetadata.StemType is the audio stem the data belongs to</para>
    /// <para>The tuple in the value holds the data (float[]) and the number of bytes per sample (long)</para>
    /// </summary>
    public static Dictionary<Metadata.StemType, StemWaveformData> WaveformData { get; private set; } = new();
    // The number of bytes per sample is needed in order to accurately play and seek through the track in PluginBassManager
    // The number of bytes can vary based on the type of audio file the user inputs, like if they use .opus, .mp3 together, etc.
    // long is just what Bass returns and I don't want to do a million casts just to make this a regular int
    // 64 bit values are actually kinda baller in my opinion so i'm not opposed 

    #region Scene Objects
    /// <summary>
    /// Line renderer that contains rightward (positive dir) waveform render
    /// </summary>
    [SerializeField] LineRenderer lineRendererMain;

    /// <summary>
    /// Line renderer that contains leftward (negative dir) waveform render
    /// </summary>
    [SerializeField] LineRenderer lineRendererMirror;

    // Note: Line renderer uses local positioning to more easily align with the screen
    // both of these line renderers combine to make a symmetrical waveform
    // and the center is hollow! so cool and unique

    /// <summary>
    /// RectTransform attached to the waveform container.
    /// </summary>
    [SerializeField] RectTransform rt;

    /// <summary>
    /// Height of the RectTransform component attached to the waveform's container GameObject.
    /// </summary>
    private float rtHeight => rt.rect.height;

    /// <summary>
    /// Panel that is always the size of the bounds of the waveform. Used to set waveform object at right distance from camera/background.
    /// </summary>
    [SerializeField] GameObject boundaryReference; // in tempo map, screen 

    [SerializeField] Strikeline strikeline;

    #endregion

    #region Display Options

    /// <summary>
    /// The y distance between each waveform point on the line renderer. Default is 0.0001.
    /// <para>Change shrink factor to modify how tight the waveform looks.</para>
    /// <para>Modified by hyperspeed and audio speed changes.</para>
    /// </summary>
    public static float ShrinkFactor // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    {
        get
        {
            return _shrinkFactor;
        }
        set
        {
            if (_shrinkFactor == value) return;
            _shrinkFactor = value;
            instance.GenerateWaveformPoints();
        }
    }
    private static float _shrinkFactor = 0.005f;

    /// <summary>
    /// Where the user is by sample count at the strikeline.
    /// <para>This corresponds to an index in the WaveformData arrays.</para>
    /// </summary>
    public static int CurrentWaveformDataPosition
    {
        get
        {
            return _wfPosition;
        }
        private set
        {
            if (_wfPosition == value) return;
            _wfPosition = value;
            instance.GenerateWaveformPoints();
        }
    }
    private static int _wfPosition = 0;

    /// <summary>
    /// Controls the length of the waveform lines in the editor. BASS-generated values are multiplied by this value to get the final coordinate result. 
    /// </summary>
    public static float Amplitude
    {
        get
        {
            return _amplitude;
        }
        set
        {
            if (_amplitude == value) return;
            _amplitude = value;
            instance.GenerateWaveformPoints();
        }
    }
    private static float _amplitude = 1;

    /// <summary>
    /// The currently displayed waveform.
    /// </summary>
    private static Metadata.StemType CurrentWaveform { get; set; }

    #endregion

    #region Unity Functions
    void Awake()
    {
        instance = this;

        // Initial waveform state is made possible by STLM's intial fire
        SongTimelineManager.TimeChanged += ChangeWaveformSegment;
    }

    void Start()
    {
        InitializeWaveformData();

        SetWaveformVisibility(false);
        // Invisible by default so that a bunch of dropdown defaulting logic isn't needed
        // Just have user select it

        CurrentWaveform = Chart.Metadata.Stems.Keys.First(); // This doesn't matter much b/c waveform is invis by default
        // This is just so that the waveform has something to generate from (avoid bricking program from error)

        var boundsRectTransform = boundaryReference.GetComponent<RectTransform>();
        rt.pivot = boundsRectTransform.pivot;
    }
    #endregion

    #region Data Initialization

    /// <summary>
    /// Create waveform data for each stem in the ChartMetadata Stems dictionary.
    /// </summary>
    void InitializeWaveformData()
    {
        foreach (var pair in Chart.Metadata.Stems)
        {
            UpdateWaveformData(pair.Key);
        }
    }

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="stem">The BASS stream to get audio samples of.</param>
    public void UpdateWaveformData(Metadata.StemType stem) // pass in file path here later
    {
        float[] stemWaveformData = AudioManager.GetAudioSamples(stem, out long bytesPerSample);

        if (WaveformData.ContainsKey(stem))
        {
            WaveformData.Remove(stem);
        } // Flush current value to allow for new one

        WaveformData.Add(stem, new(stemWaveformData, bytesPerSample));
    }

    #endregion

    #region Point Generation

    /// <summary>
    /// Generate an array of line renderer positions based on waveform audio.
    /// </summary>
    /// <returns>Vector3 array of line renderer positions</returns>
    private void GenerateWaveformPoints()
    {
        var masterWaveformData = WaveformData[CurrentWaveform].volumeData;
        var currentSampleOffset = strikeSamplePoint;

        var sampleCount = samplesPerScreen;

        // each line renderer point is a sample
        lineRendererMain.positionCount = sampleCount;
        lineRendererMirror.positionCount = sampleCount;

        Vector3[] lineRendererPositions = new Vector3[lineRendererMain.positionCount];
        float yPos = 0;

        for (int lineRendererIndex = 0; lineRendererIndex < lineRendererPositions.Length; lineRendererIndex++)
        {
            yPos = lineRendererIndex * ShrinkFactor;
            try
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(masterWaveformData[CurrentWaveformDataPosition - currentSampleOffset] * Amplitude, yPos);
            }
            catch // this happens when there is no data to pull for the waveform
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(0, yPos);
                // this way the beginning and end of the waveform will stop at the strikeline instead of screen boundaries
            }
            currentSampleOffset++; // this allows working with the waveform data from the bottom up and for CurrentWFDataPosition to be at the strikeline
        }

        lineRendererMain.SetPositions(lineRendererPositions);

        // mirror all x positions of every point
        lineRendererPositions = Array.ConvertAll(lineRendererPositions, pos => new Vector3(-pos.x, pos.y));

        lineRendererMirror.SetPositions(lineRendererPositions);

        UpdateWaveformData();
    }

    public void ChangeWaveformSegment()
    {
        Debug.Log($"{Time.frameCount}: ChangeWaveformSegment called");
        // This can use an implicit cast because song position is always rounded to 3 decimal places
        CurrentWaveformDataPosition = (int)(SongTimelineManager.SongPositionSeconds * AudioManager.SAMPLES_PER_SECOND);

        GenerateWaveformPoints();
    }

    /// <summary>
    /// Update the visible and calculated-upon waveform.
    /// </summary>
    /// <param name="stem">The stem to set to the active waveform.</param>
    public void ChangeDisplayedWaveform(Metadata.StemType stem)
    {
        SetWaveformVisibility(true);
        CurrentWaveform = stem;
        GenerateWaveformPoints();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Get the start and end second values of the visible waveform segment.
    /// </summary>
    /// <param name="startPoint">The first waveform point visible, in seconds.</param>
    /// <param name="endPoint">The last waveform point visible, in seconds</param>
    public static (double, double) GetDisplayedAudio()
    {
        var offset = strikeSamplePoint;
        // get to bottom of screen, calculate what that waveform position is in seconds
        var startPoint = (CurrentWaveformDataPosition - offset) * AudioManager.ARRAY_RESOLUTION;
        // get to bottom of screen, jump to top of screen with samplesPerScreen, calculate what that waveform position is in seconds
        var endPoint = (CurrentWaveformDataPosition - offset + samplesPerScreen) * AudioManager.ARRAY_RESOLUTION;

        return (startPoint, endPoint);
    }

    public static void GetCurrentDisplayedWaveformInfo(out int startTick, out int endTick, out double timeShown, out double startTime, out double endTime)
    {
        (startTime, endTime) = GetDisplayedAudio();
        startTick = BPM.ConvertSecondsToTickTime((float)startTime);
        endTick = BPM.ConvertSecondsToTickTime((float)endTime);
        timeShown = endTime - startTime;

        //Debug.Log($"{Time.frameCount}: {startTick}, {endTick}, {timeShown}, {startTime}, {endTime}");
    }

    static int samplesPerScreen => (int)Mathf.Round(instance.rtHeight / ShrinkFactor);
    static int strikeSamplePoint => (int)Math.Ceiling(samplesPerScreen * instance.strikeline.GetStrikelineScreenProportion());
    public static int startTick;
    public static int endTick;
    public static double timeShown;
    public static double startTime;
    public double endTime;

    void UpdateWaveformData()
    {
        GetCurrentDisplayedWaveformInfo(out startTick, out endTick, out timeShown, out startTime, out endTime);

        Chart.Refresh();
    }

    public void SetWaveformVisibility(bool isVisible)
    {
        if (isVisible) transform.position = boundaryReference.transform.position + Vector3.back;
        // ^^ In order for the waveform to be visible the container game object has to be moved in front of the background panel & vice versa
        else transform.position = boundaryReference.transform.position - 2 * Vector3.back; // 2* b/c this looks weird in the scene view otherwise
    }

    #endregion
}

public class StemWaveformData
{
    public float[] volumeData;
    public long bytesPerSample; // yk i'm not actually sure if I need this anymore but this is here juuuuuust in case

    public StemWaveformData(float[] volumeData, long bytesPerSample)
    {
        this.volumeData = volumeData;
        this.bytesPerSample = bytesPerSample;
    }
}