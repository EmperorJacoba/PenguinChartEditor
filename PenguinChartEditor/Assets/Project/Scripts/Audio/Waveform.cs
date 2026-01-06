using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Waveform : MonoBehaviour
{
    [SerializeField] bool is2D = false;
    private const float THREE_D_Y_POSITION_OFFSET = 0.01f;
    public static Waveform instance;

    public GameInstrument parentGameInstrument;

    /// <summary>
    /// Dictionary that contains waveform point data for each song stem.
    /// <para>StemType is the audio stem the data belongs to</para>
    /// <para>Data holds cached waveform float array (float[] - fairly space efficient @ 1ms per sample) and the number of bytes per sample (long)</para>
    /// </summary>
    public static Dictionary<StemType, StemWaveformData> WaveformData { get; private set; } = new();
    
    /// <summary>
    /// The currently displayed waveform.
    /// </summary>
    protected static StemType CurrentWaveform { get; set; }

    #region Scene Objects

    // Waveform is made up of two line renderers (+ dir & - dir)
    // forms symmetrical & hollow waveform 
    // main is attached to waveform object itself (use prefab with this)
    // mirror is first child
    // uses local positioning
    LineRenderer lineRendererMain;
    LineRenderer lineRendererMirror;

    /// <summary>
    /// RectTransform attached to the waveform container.
    /// </summary>
    RectTransform TwoDRectTransform
    {
        get
        {
            if (_rt == null)
            {
                _rt = GetComponent<RectTransform>();
            }
            return _rt;
        }
    }
    RectTransform _rt;

    /// <summary>
    /// Height of the RectTransform component attached to the waveform's container GameObject.
    /// </summary>
    private float TwoDReferenceHeight => TwoDRectTransform.rect.height;

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
            GenerateWaveformPoints();
        }
    }
    private static float _shrinkFactor = 0.005f;
    private static float _shrinkFactor3Dadjustment => _shrinkFactor * 5;

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
            GenerateWaveformPoints();
        }
    }
    private static float _amplitude = 1;
    private static float _amplitude3DAdjustment => _amplitude * 5;

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            if (Visible == value) return;
            gameObject.SetActive(value);
        }
    }

    #endregion

    #region Unity Functions

    protected virtual void Awake()
    {
        lineRendererMain = GetComponent<LineRenderer>();
        lineRendererMirror = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();

        PointUpdateNeeded += x => ApplyGeneratedPositions(x);

        if (is2D) instance = this;
    }

    #endregion

    #region Data Initialization

    /// <summary>
    /// Create waveform data for each stem in the ChartMetadata Stems dictionary.
    /// </summary>
    public static void InitializeWaveformData()
    {
        WaveformData = new();
        Parallel.ForEach(Chart.Metadata.StemPaths.Keys, item => UpdateWaveformData(item));
    }

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="stem">The BASS stream to get audio samples of.</param>
    static void UpdateWaveformData(StemType stem) // pass in file path here later
    {
        float[] stemWaveformData = AudioManager.GetAudioSamples(stem, out long bytesPerSample);

        WaveformData.Add(stem, new(stemWaveformData, bytesPerSample));
    }

    #endregion

    #region Properties

    static int GetSampleCapacity()
    {
        return Chart.instance.SceneDetails.is2D ?
            (int)Mathf.Round(instance.TwoDReferenceHeight / ShrinkFactor) :
            (int)Mathf.Round(Highway3D.highwayLength / (ShrinkFactor));
    }
    static int GetStrikelineSamplePosition()
    {
        return Chart.instance.SceneDetails.is2D ?
            (int)Math.Ceiling(GetSampleCapacity() * Strikeline2D.instance.GetStrikelineProportion()) :
            (int)Math.Ceiling(GetSampleCapacity() * Strikeline3D.GetAnyStrikelineProportion());
    }

    public static int startTick;
    public static int songPositionTicks;
    public static int endTick;
    public static double timeShown;
    public static double startTime;
    public static double endTime;



    #endregion

    #region Point Generation

    delegate void WaveformDataUpdated(Vector3[] positions);
    static event WaveformDataUpdated PointUpdateNeeded;
    public static void GenerateWaveformPoints()
    {
        // This can use an implicit cast because song position is always rounded to 3 decimal places
        var currentWaveformDataPosition = (int)(SongTime.SongPositionSeconds * AudioManager.SAMPLES_PER_SECOND);

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
        var startSampleIndex = currentWaveformDataPosition - GetStrikelineSamplePosition();

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

            // Waveform is rendered slightly differently in 2D (TempoMap) versus 3D (chart & others)
            // Track operates on X & Y directions in 2D, X & Z directions in 3D. Thus, the branching.
            if (Chart.instance.SceneDetails.is2D)
            {
                lineRendererPositions[lineRendererIndex] = new(xPosition, incrementPosition);
            }
            else
            {
                lineRendererPositions[lineRendererIndex] = new(xPosition, THREE_D_Y_POSITION_OFFSET, incrementPosition);
            }
        }

        CacheWaveformDetails(
            startTimeSeconds: startSampleIndex * AudioManager.ARRAY_RESOLUTION,
            positionTimeSeconds: currentWaveformDataPosition * AudioManager.ARRAY_RESOLUTION,
            endTimeSeconds: (startSampleIndex + sampleCount) * AudioManager.ARRAY_RESOLUTION
            );

        PointUpdateNeeded?.Invoke(lineRendererPositions);
    }

    private void ApplyGeneratedPositions(Vector3[] positions)
    {
        if (!WaveformData.ContainsKey(CurrentWaveform))
        {
            Visible = false;
            return;
        }

        Visible = true;

        lineRendererMain.positionCount = lineRendererMirror.positionCount = positions.Length;
        lineRendererMain.SetPositions(positions);

        // mirror all x positions of every point
        positions = Array.ConvertAll(positions, pos => new Vector3(-pos.x, pos.y, pos.z));
        lineRendererMirror.SetPositions(positions);
    }

    static void CacheWaveformDetails(double startTimeSeconds, double positionTimeSeconds, double endTimeSeconds)
    {
        startTime = startTimeSeconds;
        endTime = endTimeSeconds;
        timeShown = endTimeSeconds - startTimeSeconds;

        startTick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)startTimeSeconds);
        songPositionTicks = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)positionTimeSeconds);
        endTick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)endTimeSeconds);

        CacheTimeDetails();
    }

    static void CacheTimeDetails()
    {
        tickSecondValueMatch.Clear();
        tickSecondValueMatch[startTick] = new(Chart.SyncTrackInstrument.GetSecondsPerTickAtTick(startTick), accumulatedSeconds: 0);

        var activeEventTick = Chart.SyncTrackInstrument.TempoEvents.GetNextTickEventInLane(startTick, inclusive: false);
        var lastActiveTick = startTick;

        while (activeEventTick < endTick)
        {
            if (activeEventTick == LaneSet<BPMData>.NO_TICK_EVENT)
            {
                break;
            }
            tickSecondValueMatch[activeEventTick] =
                new(Chart.SyncTrackInstrument.GetSecondsPerTickAtTick(activeEventTick),
                accumulatedSeconds: tickSecondValueMatch[lastActiveTick].secondsPerTick * (activeEventTick - lastActiveTick) + tickSecondValueMatch[lastActiveTick].accumulatedSeconds
                );

            lastActiveTick = activeEventTick;
            activeEventTick = Chart.SyncTrackInstrument.TempoEvents.GetNextTickEventInLane(activeEventTick, inclusive: false);
        }

        tickPositions = tickSecondValueMatch.Keys.ToArray();
    }
    
    // Key: Tick start point.
    static SortedDictionary<int, CachedTimestampPosition> tickSecondValueMatch = new();
    static int[] tickPositions;
    public static double GetWaveformRatio(int tick)
    {
        if (tick < startTick) return 0;
        if (tick > endTick) return 1;

        int i = 0;
        while (i + 1 < tickPositions.Length && tickPositions[i+1] <= tick)
        {
            i++;
        }

        int key = tickPositions[i];
        var activeData = tickSecondValueMatch[key];

        return (activeData.accumulatedSeconds + (activeData.secondsPerTick * (tick - key))) / timeShown;
    }

    public static double GetWaveformRatio(int tick, int tickDuration)
    {
        return GetWaveformRatio(tick + tickDuration) - GetWaveformRatio(tick);
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

public struct CachedTimestampPosition
{
    public double secondsPerTick;
    public double accumulatedSeconds;

    public CachedTimestampPosition(double secondsPerTick, double accumulatedSeconds)
    {
        this.secondsPerTick = secondsPerTick;
        this.accumulatedSeconds = accumulatedSeconds;
    }
}