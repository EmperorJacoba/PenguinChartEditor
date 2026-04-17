using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]

// Waveform is the central part of Penguin that ties everything together. Event spawning is heavily tied to the waveform
// and getting most information about timing/positioning/etc is received as a direct result of contacting Waveform.
// It's set up like this because the Waveform was the first thing I added as I thought it was the most essential part
// of a charting program (as tempo mapping is almost entirely tied to one's ability to interpret a waveform). Waveform
// also sets the spawning boundaries as a natural result of how it is generated.
public class Waveform : MonoBehaviour
{
    #region Constants
    private const float THREE_D_Y_POSITION_OFFSET = 0.01f;
    #endregion

    /// <remarks>
    /// Holds cached volume data associated with a specific stem. 
    /// </remarks>
    private static Dictionary<StemType, StemWaveformData> WaveformData { get; set; } = new();
    private static StemType CurrentWaveform { get; set; }
    
    #region Scene Objects

    // Waveform is made up of two line renderers (+ dir & - dir)
    // forms symmetrical & hollow waveform 
    // main is attached to waveform object itself (use prefab with this)
    // mirror is first child
    // uses local positioning
    private LineRenderer lineRendererMain;
    private LineRenderer lineRendererMirror;
    
    public GameInstrument parentGameInstrument;
    
    #endregion

    #region Display Options

    /// <summary>
    /// The point-to-point distance between each waveform point on the line renderer.
    /// <para>Change shrink factor to modify how tight the waveform looks.</para>
    /// <para>Modified by hyperspeed and audio speed changes.</para>
    /// </summary>
    public static float ShrinkFactor // Needed to compress the points into something legible (y value * shrinkFactor = y position)
    {
        get
        {
            // Relic from old 2D system. *5 was old 2D->3D conversion factor. 
            return _shrinkFactor * 5;
        }
        set
        {
            if (Mathf.Approximately(_shrinkFactor, value)) return;
            _shrinkFactor = value;
            GenerateWaveformPoints();
        }
    }
    private static float _shrinkFactor = 0.005f;

    /// <summary>
    /// Controls the length of the waveform lines in the editor. BASS-generated values are multiplied by this value to get the final coordinate result. 
    /// </summary>
    public static float Amplitude
    {
        get
        {
            // Relic from old 2D system. *5 was old 2D->3D conversion factor.
            return _amplitude * 5;
        }
        set
        {
            if (Mathf.Approximately(_amplitude, value)) return;
            _amplitude = value;
            GenerateWaveformPoints();
        }
    }
    private static float _amplitude = 1.0f;

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

    private void Awake()
    {
        lineRendererMain = GetComponent<LineRenderer>();
        lineRendererMirror = gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
    }

    private void OnEnable()
    {
        PointUpdateNeeded += ApplyGeneratedPositions;
    }

    private void OnDestroy()
    {
        PointUpdateNeeded -= ApplyGeneratedPositions;
    }

    #endregion

    #region Data Initialization

    /// <summary>
    /// Create waveform data for each stem in the ChartMetadata Stems dictionary.
    /// </summary>
    public static void InitializeWaveformData()
    {
        ConcurrentDictionary<StemType, StemWaveformData> threadSafeDict = new();
        Parallel.ForEach(Chart.Metadata.StemPaths.Keys, item =>
        {
            var kvp = UpdateWaveformData(item);
            threadSafeDict.AddOrUpdate(kvp.Key, kvp.Value, (key, value) => value);
        });

        WaveformData = threadSafeDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Update waveform data to a new audio file.
    /// </summary>
    /// <param name="stem">The BASS stream to get audio samples of.</param>
    private static KeyValuePair<StemType, StemWaveformData> UpdateWaveformData(StemType stem)
    {
        float[] stemWaveformData = AudioManager.GetAllAudioSamples(stem);

        return new KeyValuePair<StemType, StemWaveformData>(stem, new StemWaveformData(stemWaveformData));
    }

    #endregion

    #region Properties

    private static int GetSampleCapacity() => (int)Mathf.Round(Highway3D.highwayLength / (ShrinkFactor));
    private static int GetStrikelineSamplePosition() => (int)Math.Ceiling(GetSampleCapacity() * Strikeline3D.GetAnyStrikelineProportion());

    public static int startTick;
    public static int songPositionTicks;
    public static int endTick;
    public static double timeShown;
    public static double startTime;
    public static double endTime;
    public static double negativeTimePercentageOffset;

    #endregion

    #region Point Generation

    private delegate void WaveformDataUpdated(Vector3[] positions);

    private static event WaveformDataUpdated PointUpdateNeeded;
    public static void GenerateWaveformPoints()
    {
        // This can use an implicit cast because song position is always rounded to 3 decimal places
        var currentWaveformDataPosition = (int)(SongTime.SongPositionSeconds * AudioManager.SAMPLES_PER_SECOND);

        var waveformData = WaveformData.TryGetValue(CurrentWaveform, out var stemWaveformData)
            ? stemWaveformData.volumeData
            : Array.Empty<float>();
        
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

            lineRendererPositions[lineRendererIndex] =
                new Vector3(xPosition, THREE_D_Y_POSITION_OFFSET, incrementPosition);
        }

        CacheWaveformDetails(
            startTimeSeconds: startSampleIndex * AudioManager.ARRAY_RESOLUTION,
            positionTimeSeconds: currentWaveformDataPosition * AudioManager.ARRAY_RESOLUTION,
            endTimeSeconds: (startSampleIndex + sampleCount) * AudioManager.ARRAY_RESOLUTION
            );

        PointUpdateNeeded?.Invoke(lineRendererPositions);
    }
    
    // The idea is that there are multiple waveforms all showing the same data. Function above calculates the points,
    // this sets every Waveform's points based on that calculation. Change this to calculate individual points here
    // if people need different waveforms on each instrument. Careful with the implementation though because that
    // could get real inefficient real quick. Don't recommend that feature, personally.
    private void ApplyGeneratedPositions(Vector3[] positions)
    {
        if (!WaveformData.ContainsKey(CurrentWaveform))
        {
            Visible = false;
            return;
        }

        Visible = true;
        float xOffset = 0;
        
        xOffset = parentGameInstrument.GetGlobalCenterHighwayPosition();
        positions = Array.ConvertAll(positions, pos => new Vector3(pos.x + xOffset, pos.y, pos.z));

        lineRendererMain.positionCount = lineRendererMirror.positionCount = positions.Length;
        lineRendererMain.SetPositions(positions);

        // mirror all x positions of every point
        positions = Array.ConvertAll(positions, pos => new Vector3(-pos.x + 2 * xOffset, pos.y, pos.z));
        lineRendererMirror.SetPositions(positions);
    }

    private static void CacheWaveformDetails(double startTimeSeconds, double positionTimeSeconds, double endTimeSeconds)
    {
        startTime = startTimeSeconds;
        endTime = endTimeSeconds;
        timeShown = endTimeSeconds - startTimeSeconds;
        negativeTimePercentageOffset = startTime < 0 ? -startTime / timeShown : 0;

        startTick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)startTimeSeconds);
        songPositionTicks = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)positionTimeSeconds);
        endTick = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)endTimeSeconds);

        CacheTimeDetails();
    }

    private static void CacheTimeDetails()
    {
        tickSecondValueMatch.Clear();
        tickSecondValueMatch[startTick] = new CachedTimestampPosition(Chart.SyncTrackInstrument.GetSecondsPerTickAtTick(startTick), accumulatedSeconds: 0);

        var activeEventTick = Chart.SyncTrackInstrument.TempoEvents.GetNextTickEventInLane(startTick, inclusive: false);
        var lastActiveTick = startTick;

        while (activeEventTick < endTick)
        {
            if (activeEventTick == LaneSet<BPMData>.NO_TICK_EVENT)
            {
                break;
            }
            tickSecondValueMatch[activeEventTick] =
                new CachedTimestampPosition(Chart.SyncTrackInstrument.GetSecondsPerTickAtTick(activeEventTick),
                accumulatedSeconds: tickSecondValueMatch[lastActiveTick].secondsPerTick * (activeEventTick - lastActiveTick) + tickSecondValueMatch[lastActiveTick].accumulatedSeconds
                );

            lastActiveTick = activeEventTick;
            activeEventTick = Chart.SyncTrackInstrument.TempoEvents.GetNextTickEventInLane(activeEventTick, inclusive: false);
        }

        tickPositions = tickSecondValueMatch.Keys.ToArray();
    }
    
    // Key: Tick start point.
    private static SortedDictionary<int, CachedTimestampPosition> tickSecondValueMatch = new();
    private static int[] tickPositions;
    public static double GetWaveformRatio(int tick, bool needsNegativeFallbackPosition = false)
    {
        if (tick < startTick)
        {
            return needsNegativeFallbackPosition ? ManualGetWaveformRatio(tick) : -1.0f;
        }
        if (tick >= endTick) return 1.0f;

        int i = 0;
        while (i + 1 < tickPositions.Length && tickPositions[i+1] <= tick)
        {
            i++;
        }

        int key = tickPositions[i];
        var activeData = tickSecondValueMatch[key];

        // Note from later: Is this still true? I don't think it is
        // If the waveform's startTick is negative (e.g. when at the beginning of the song)
        // negativeTimePercentageOffset > 0 and this corrects for the void of data for the negative portions of the track]
        // Basically stores the start point of data on the track.
        // This formula w/o the offset will start generation at the beginning of the track and display incorrect data until startTime > 0.
        // The negative time offset is cached in CacheWaveformDetails(), and will be 0 in any case not described above.
        return (activeData.accumulatedSeconds + (activeData.secondsPerTick * (tick - key))) / timeShown + negativeTimePercentageOffset;
    }
    
    /// <summary>
    /// Very inefficient alternative method as an alternative for GetWaveformRatio. Needed for when the position of an event
    /// in the negative is significant. 
    /// </summary>
    /// <param name="tick"></param>
    /// <returns></returns>
    public static double ManualGetWaveformRatio(int tick)
    {
        return (Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick) - startTime) / timeShown;
    }

    public static double GetWaveformRatio(int tick, int tickDuration)
    {
        return GetWaveformRatio(tick + tickDuration) - GetWaveformRatio(tick);
    }

    /// <summary>
    /// Update the visible and calculated-upon waveform.
    /// </summary>
    /// <param name="stem">The stem to set to the active waveform.</param>
    public static void ChangeDisplayedWaveform(StemType stem)
    {
        CurrentWaveform = stem;
        GenerateWaveformPoints();
    }

    #endregion
}

// Used to hold other data. Beats a magic type I guess?
public class StemWaveformData
{
    public float[] volumeData;

    public StemWaveformData(float[] volumeData)
    {
        this.volumeData = volumeData;
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