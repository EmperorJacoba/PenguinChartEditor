using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Waveform : MonoBehaviour
{
    public static Waveform instance;

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
    LineRenderer lineRendererMain;

    /// <summary>
    /// Line renderer that contains leftward (negative dir) waveform render
    /// </summary>
    LineRenderer lineRendererMirror;

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
            return Chart.instance.SceneDetails.is2D ? _shrinkFactor : _shrinkFactor3Dadjustment;
        }
        set
        {
            if (_shrinkFactor == value) return;
            _shrinkFactor = value;
            instance.GenerateWaveformPoints();
        }
    }
    private static float _shrinkFactor = 0.005f;
    private static float _shrinkFactor3Dadjustment => _shrinkFactor * 5;

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
            return Chart.instance.SceneDetails.is2D ? _amplitude : _amplitude3DAdjustment;
        }
        set
        {
            if (_amplitude == value) return;
            _amplitude = value;
            instance.GenerateWaveformPoints();
        }
    }
    private static float _amplitude = 1;
    private static float _amplitude3DAdjustment = _amplitude * 5;

    /// <summary>
    /// The currently displayed waveform.
    /// </summary>
    protected static StemType CurrentWaveform { get; set; }

    #endregion

    #region Unity Functions
    protected virtual void Awake()
    {
        instance = this;
        lineRendererMain = GetComponent<LineRenderer>();
        lineRendererMirror = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();

        // Initial waveform state is made possible by STLM's initial fire
        SongTime.TimeChanged += ChangeWaveformSegment;
    }

    protected void Start()
    {
        InitializeWaveformData();

        // Invisible by default so that a bunch of dropdown defaulting logic isn't needed
        // Just have user select it
        Visible = false;

        CurrentWaveform = Chart.Metadata.StemPaths.Keys.First(); // This doesn't matter much b/c waveform is invis by default
        // This is just so that the waveform has something to generate from (avoid bricking program from error)
        if (Chart.instance.SceneDetails.is2D) Init2D();
    }

    void Init2D()
    {
        var boundsRectTransform = (RectTransform)Chart.instance.SceneDetails.highway;
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

        var sampleCount = GetSampleCapacity();
        var startSampleIndex = CurrentWaveformDataPosition - GetStrikelineSamplePosition();

        // each line renderer point is a sample
        lineRendererMain.positionCount = sampleCount;
        lineRendererMirror.positionCount = sampleCount;
        Vector3[] lineRendererPositions = new Vector3[sampleCount];

        for (int lineRendererIndex = 0; lineRendererIndex < lineRendererPositions.Length; lineRendererIndex++)
        {
            int waveformIndex = startSampleIndex + lineRendererIndex;
            float incrementPosition = lineRendererIndex * ShrinkFactor;

            float xPosition = 0;
            if (waveformIndex >= 0 && waveformIndex < waveformData.Length)
            {
                xPosition = waveformData[waveformIndex] * Amplitude;
            }

            if (Chart.instance.SceneDetails.is2D)
            {
                lineRendererPositions[lineRendererIndex] = new(xPosition, incrementPosition);
            }
            else
            {
                lineRendererPositions[lineRendererIndex] = new(xPosition, 0.01f, incrementPosition);
            }
        }

        lineRendererMain.SetPositions(lineRendererPositions);

        // mirror all x positions of every point
        lineRendererPositions = Array.ConvertAll(lineRendererPositions, pos => new Vector3(-pos.x, pos.y, pos.z));

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
        var offset = GetStrikelineSamplePosition();
        // get to bottom of screen, calculate what that waveform position is in seconds
        var startPoint = (CurrentWaveformDataPosition - offset) * AudioManager.ARRAY_RESOLUTION;
        // get to bottom of screen, jump to top of screen with samplesPerScreen, calculate what that waveform position is in seconds
        var endPoint = (CurrentWaveformDataPosition - offset + GetSampleCapacity()) * AudioManager.ARRAY_RESOLUTION;

        return (startPoint, endPoint);
    }

    public void CacheWaveformDetails()
    {
        (startTime, endTime) = GetDisplayedAudio();
        startTick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)startTime);
        endTick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)endTime);
        songPositionTicks = Chart.SyncTrackInstrument.ConvertSecondsToTickTime(CurrentWaveformDataPosition * (float)AudioManager.ARRAY_RESOLUTION);
        timeShown = endTime - startTime;
        ticksShown = endTick - startTick;

        //Debug.Log($"{Time.frameCount}: {startTick}, {endTick}, {timeShown}, {startTime}, {endTime}");
    }

    protected virtual int GetSampleCapacity()
    {
        return Chart.instance.SceneDetails.is2D ? 
            (int)Mathf.Round(instance.rtHeight / ShrinkFactor) :
            (int)Mathf.Round(Chart.instance.SceneDetails.HighwayLength / (ShrinkFactor));
    }
    protected int GetStrikelineSamplePosition()
    {
        return Chart.instance.SceneDetails.is2D ? 
            (int)Math.Ceiling(GetSampleCapacity() * Strikeline.instance.GetStrikelineProportion()) :
            (int)Math.Ceiling(GetSampleCapacity() * Strikeline3D.instance.GetStrikelineProportion());
    }


    public static int ticksShown;
    public static int startTick;
    public static int songPositionTicks;
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
        CacheWaveformDetails();

        Chart.Refresh();
    }

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }

    public static double GetWaveformRatio(int tick)
    {
        return (Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick) - startTime) / timeShown;
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