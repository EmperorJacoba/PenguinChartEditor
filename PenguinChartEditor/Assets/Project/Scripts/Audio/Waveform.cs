using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Waveform : MonoBehaviour
{
    static Waveform instance;

    /// <summary>
    /// Dictionary that contains waveform point data for each song stem.
    /// <para>StemType is the audio stem the data belongs to</para>
    /// <para>The tuple in the value holds the data (float[]) and the number of bytes per sample (long)</para>
    /// </summary>
    public static Dictionary<StemType, StemWaveformData> WaveformData { get; private set; } = new();

    #region Scene Objects
    /// <summary>
    /// Line renderer that contains rightward (positive dir) waveform render
    /// </summary>
    [SerializeField] protected LineRenderer lineRendererMain;

    /// <summary>
    /// Line renderer that contains leftward (negative dir) waveform render
    /// </summary>
    [SerializeField] protected LineRenderer lineRendererMirror;

    // Note: Line renderer uses local positioning to more easily align with the screen
    // both of these line renderers combine to make a symmetrical waveform
    // and the center is hollow! so cool and unique

    /// <summary>
    /// RectTransform attached to the waveform container.
    /// </summary>
    [SerializeField] RectTransform rt_2DOnly;

    /// <summary>
    /// Height of the RectTransform component attached to the waveform's container GameObject.
    /// </summary>
    private float rtHeight => rt_2DOnly.rect.height;

    /// <summary>
    /// Panel that is always the size of the bounds of the waveform. Used to set waveform object at right distance from camera/background.
    /// </summary>
    [SerializeField] GameObject boundaryReference; // in tempo map, screen 

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
            // GenerateWaveformPoints is already called elsewhere when this is changed
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
    protected static StemType CurrentWaveform { get; set; }

    #endregion

    #region Unity Functions
    protected virtual void Awake()
    {
        instance = this;

        // Initial waveform state is made possible by STLM's initial fire
        SongTime.TimeChanged += ChangeWaveformSegment;
    }

    protected void Start()
    {
        InitializeWaveformData();

        // Invisible by default so that a bunch of dropdown defaulting logic isn't needed
        // Just have user select it
        SetWaveformVisibility(true);

        CurrentWaveform = Chart.Metadata.StemPaths.Keys.First(); // This doesn't matter much b/c waveform is invis by default
        // This is just so that the waveform has something to generate from (avoid bricking program from error)
        if (wf2D) Init2D();
    }
    protected bool wf2D = true;

    void Init2D()
    {
        var boundsRectTransform = boundaryReference.GetComponent<RectTransform>();
        rt_2DOnly.pivot = boundsRectTransform.pivot;
    }
    #endregion

    #region Data Initialization

    /// <summary>
    /// Create waveform data for each stem in the ChartMetadata Stems dictionary.
    /// </summary>
    void InitializeWaveformData()
    {
        WaveformData = new();
        Parallel.ForEach(Chart.Metadata.StemPaths.Keys, item => UpdateWaveformData(item));
    }

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="stem">The BASS stream to get audio samples of.</param>
    void UpdateWaveformData(StemType stem) // pass in file path here later
    {
        float[] stemWaveformData = AudioManager.GetAudioSamples(stem, out long bytesPerSample);

        WaveformData.Add(stem, new(stemWaveformData, bytesPerSample));
    }

    #endregion

    #region Point Generation

    /// <summary>
    /// Generate an array of line renderer positions based on waveform audio.
    /// </summary>
    /// <returns>Vector3 array of line renderer positions</returns>
    protected virtual void GenerateWaveformPoints()
    {
        float[] waveformData;
        if (WaveformData.ContainsKey(CurrentWaveform))
        {
            waveformData = WaveformData[CurrentWaveform].volumeData;
        }
        else
        {
            // this is to generate waveform data even if there is either
            // a) no data available (no audio loaded)
            // or b) a call when CurrentWaveform = 0 (none) occurs.
            // this lets the for loop below execute because it can't without SOMETHING in waveformData.
            // the if statement in that loop is always true when an empty float[] exists in waveformData,
            // so it accurately represents no data (even though the "waveform" is actually behind the track)
            waveformData = new float[0];
        }

        var sampleCount = samplesPerBoundary;
        var startSampleIndex = CurrentWaveformDataPosition - strikeSamplePoint;

        // each line renderer point is a sample
        lineRendererMain.positionCount = sampleCount;
        lineRendererMirror.positionCount = sampleCount;

        Vector3[] lineRendererPositions = new Vector3[lineRendererMain.positionCount];
        float yPos = 0;

        for (int lineRendererIndex = 0; lineRendererIndex < lineRendererPositions.Length; lineRendererIndex++)
        {
            yPos = lineRendererIndex * ShrinkFactor;
            var waveformIndex = startSampleIndex + lineRendererIndex;

            if (waveformIndex < 0 || waveformIndex >= waveformData.Length)
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(0, yPos);
                continue;
            }
            lineRendererPositions[lineRendererIndex] = new(waveformData[waveformIndex] * Amplitude, yPos);
        }

        lineRendererMain.SetPositions(lineRendererPositions);

        // mirror all x positions of every point
        lineRendererPositions = Array.ConvertAll(lineRendererPositions, pos => new Vector3(-pos.x, pos.y));

        lineRendererMirror.SetPositions(lineRendererPositions);

        UpdateWaveformData();
    }

    public void ChangeWaveformSegment()
    {
        // This can use an implicit cast because song position is always rounded to 3 decimal places
        CurrentWaveformDataPosition = (int)(SongTime.SongPositionSeconds * AudioManager.SAMPLES_PER_SECOND);

        GenerateWaveformPoints();
    }

    /// <summary>
    /// Update the visible and calculated-upon waveform.
    /// </summary>
    /// <param name="stem">The stem to set to the active waveform.</param>
    public void ChangeDisplayedWaveform(StemType stem)
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
    (double, double) GetDisplayedAudio()
    {
        var offset = strikeSamplePoint;
        // get to bottom of screen, calculate what that waveform position is in seconds
        var startPoint = (CurrentWaveformDataPosition - offset) * AudioManager.ARRAY_RESOLUTION;
        // get to bottom of screen, jump to top of screen with samplesPerScreen, calculate what that waveform position is in seconds
        var endPoint = (CurrentWaveformDataPosition - offset + samplesPerBoundary) * AudioManager.ARRAY_RESOLUTION;

        return (startPoint, endPoint);
    }

    public void GetCurrentDisplayedWaveformInfo(out int startTick, out int endTick, out double timeShown, out double startTime, out double endTime, out int ticksShown)
    {
        (startTime, endTime) = GetDisplayedAudio();
        startTick = Tempo.ConvertSecondsToTickTime((float)startTime);
        endTick = Tempo.ConvertSecondsToTickTime((float)endTime);
        timeShown = endTime - startTime;
        ticksShown = endTick - startTick;
        

        //Debug.Log($"{Time.frameCount}: {startTick}, {endTick}, {timeShown}, {startTime}, {endTime}");
    }

    protected virtual int samplesPerBoundary => (int)Mathf.Round(instance.rtHeight / ShrinkFactor);
    protected virtual int strikeSamplePoint => (int)Math.Ceiling(samplesPerBoundary * Strikeline.instance.GetStrikelineProportion());
    public static int ticksShown;
    public static int startTick;
    public static int endTick;
    public static double timeShown;
    public static double startTime;
    public static double endTime;

    /// <summary>
    /// Caches the current properties of the displayed waveform segment and refreshes data.
    /// <para>Should be called after generating waveform points.</para>
    /// </summary>
    public void UpdateWaveformData()
    {
        GetCurrentDisplayedWaveformInfo(out startTick, out endTick, out timeShown, out startTime, out endTime, out ticksShown);

        Chart.Refresh();
    }

    public virtual void SetWaveformVisibility(bool isVisible)
    {
        if (isVisible) transform.position = boundaryReference.transform.position + Vector3.back;
        // ^^ In order for the waveform to be visible the container game object has to be moved in front of the background panel & vice versa
        else transform.position = boundaryReference.transform.position - 2 * Vector3.back; // 2* b/c this looks weird in the scene view otherwise
    }

    public static double GetWaveformRatio(int tick)
    {
        return (Tempo.ConvertTickTimeToSeconds(tick) - startTime) / timeShown;
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