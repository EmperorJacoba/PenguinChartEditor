using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SongTimelineManager : MonoBehaviour
{
    static InputMap inputMap;
    WaveformManager waveformManager;

    // Needed for delta calculations when scrolling using MMB
    private float initialMouseY = float.NaN;
    private float currentMouseY;

    public const int SECONDS_PER_MINUTE = 60;

    /// <summary>
    /// The current timestamp of the song at the strikeline.
    /// </summary>
    public static double SongPositionSeconds
    {
        get
        {
            return _songPos;
        }
        private set
        {
            if (_songPos == value) return;
            value = Math.Round(value, 3); // So that CurrentWFDataPosition comes out clean
            _songPos = value;

            TimeChanged?.Invoke();
        }
    }
    private static double _songPos = 0; 

    /// <summary>
    /// Dictionary that contains tempo changes and corresponding tick time positions. 
    /// <para> Key = Tick-time position. Value = BPM to three decimal places, time-second value of the tempo change. </para>
    /// <para>Example: 192 = 102.201, 0.237</para>
    /// <remarks>When writing to file, multiply BPM value by 100 to get proper .chart format (where example would show as 192 = B 102201)</remarks>
    /// </summary>
    public SortedDictionary<int, (float, float)> TempoEvents {get; set;} // This is sorted dict so that it is easier to read & write tempo changes

    public delegate void TimeChangedDelegate();

    /// <summary>
    /// Event that fires whenever the song position changes.
    /// </summary>
    public static event TimeChangedDelegate TimeChanged;

    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();
        TempoEvents = new();

        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ScrollTrack.performed += scrollChange => ChangeTime(scrollChange.ReadValue<float>());

        inputMap.Charting.MiddleScrollMousePos.performed += x => currentMouseY = x.ReadValue<Vector2>().y;
        inputMap.Charting.MiddleScrollMousePos.Disable(); // This is disabled immediately so that it's not running when it's not needed

        inputMap.Charting.MiddleMouseClick.started += x => ChangeMiddleClick(true);
        inputMap.Charting.MiddleMouseClick.canceled += x => ChangeMiddleClick(false);
    }

    void Start()
    {
        (TempoEvents, TempoManager.TimeSignatureEvents) = ChartParser.GetSyncTrackEventDicts("C:/_PCE_files/TestAudioFiles/Burning.chart");
        if (TempoEvents.Count == 0) // if there is no data to load in 
        {
            TempoEvents.Add(0, (120.0f, 0)); // add placeholder bpm
        }
    }

    void Update()
    {
        if (inputMap.Charting.MiddleScrollMousePos.enabled)
        {
            ChangeTime(currentMouseY - initialMouseY, true);
            // This runs every frame to get that smooth scrolling effect like on webpages and such
            // If this ran when the mouse was moved then it would be super jumpy
        }

        // No funky calculations needed, just update the song position every frame
        // Add calibration here later on
        if (PluginBassManager.AudioPlaying)
        {
            SongPositionSeconds = PluginBassManager.GetCurrentAudioPosition();
        }
    }

    public static void ToggleChartingInputMap()
    {
        if (inputMap.Charting.enabled) inputMap.Charting.Disable();
        else inputMap.Charting.Enable();
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
    /// Change the timestamp of the song from a specified scroll change.
    /// </summary>
    /// <param name="scrollChange"></param>
    /// <param name="middleClick"></param>
    void ChangeTime(float scrollChange, bool middleClick = false)
    {
        if (float.IsNaN(scrollChange)) return; // for some reason when the input map is reenabled it passes NaN into this function so we will be having none of that thank you 

        // If it's a middle click, the delta value is wayyy too large so this is a solution FOR NOW
        var scrollSuppressant = 1;
        if (middleClick) scrollSuppressant = 50;
        SongPositionSeconds += scrollChange / (UserSettings.Sensitivity * scrollSuppressant);

        // Clamp position to within the length of the song
        if (SongPositionSeconds < 0)
        {
            SongPositionSeconds = 0;
        }
        else if (SongPositionSeconds >= PluginBassManager.SongLength)
        {
            SongPositionSeconds = PluginBassManager.SongLength - 0.005;
        }
    }

    public int ConvertSecondsToTickTime(float timestamp)
    {
        // Get parallel lists of the tick-time events and time-second values so that value found with seconds can be converted to a tick-time event
        var tempoTickTimeEvents = TempoEvents.Keys.ToList();
        var tempoTimeSecondEvents = TempoEvents.Values.Select(x => x.Item2).ToList();

        // Attempt a binary search for the current timestamp, 
        // which will return a bitwise complement of the index of the next highest timesecond value 
        // OR tempoTimeSecondEvents.Count if there are no more elements
        var index = tempoTimeSecondEvents.BinarySearch(timestamp);

        int lastTickEvent;
        if (index <= 0) // bitwise complement is negative or zero
        {
            // modify index if the found timestamp is at the end of the array (last tempo event)
            if (~index == tempoTimeSecondEvents.Count) index = tempoTimeSecondEvents.Count - 1;
            // else just get the index proper 
            else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before to properly render beatlines
            try
            {
                lastTickEvent = tempoTickTimeEvents[index]; 
            }
            catch
            {
                lastTickEvent = tempoTickTimeEvents[0]; // if ~index - 1 is -1, then the index should be itself
            }
        }
        else 
        {
            lastTickEvent = tempoTickTimeEvents[index];
        }

        return Mathf.RoundToInt((TempoManager.PLACEHOLDER_RESOLUTION * TempoEvents[lastTickEvent].Item1 * (TempoEvents[lastTickEvent].Item2 - timestamp) / SECONDS_PER_MINUTE) + TempoEvents[lastTickEvent].Item2);
    }

    public float ConvertTickTimeToSeconds(int ticktime)
    {
        var tickTimeKeys = TempoEvents.Keys.ToList();

        var index = tickTimeKeys.BinarySearch(ticktime);

        int lastTickEvent;
        if (index < 0) // bitwise complement is negative
        {
            // modify index if the found timestamp is at the end of the array (last tempo event)
            if (~index == tickTimeKeys.Count) index = tickTimeKeys.Count - 1;
            // else just get the index proper 
            else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before to properly render beatlines
            try
            {
                lastTickEvent = tickTimeKeys[index]; 
            }
            catch
            {
                lastTickEvent = tickTimeKeys[0]; // if ~index - 1 is -1, then the index should be itself
            }
        }
        else 
        {
            lastTickEvent = tickTimeKeys[index];
        }

        // Formula from .chart format specifications
        return (ticktime - lastTickEvent) / TempoManager.PLACEHOLDER_RESOLUTION * SECONDS_PER_MINUTE / TempoEvents[lastTickEvent].Item1;
    }

    /// <summary>
    /// Get the tick-time event directly before a given timestamp from TempoEvents.
    /// </summary>
    /// <param name="currentTimestamp">The timestamp to get the tick-time event from.</param>
    /// <returns>The tick-time event before currentTimestamp in TempoEvents</returns>
    private int FindPrecedingTempoEvent(double currentTimestamp)
    {
        // As much as I would love to do this based on a tick-time event, 
        // a tick-time event is not guaranteed to exist in a given time period
        // so we have to get the last tempo using time-second timestamps
        // basically, have a timestamp, and find the tick-time key of the 
        // time-second value that would fall directly before the current timestamp, 
        // if the current timestamp was in TempoEvents

        // Get parallel lists of the tick-time events and time-second values so that value found with seconds can be converted to a tick-time event
        var tempoTickTimeEvents = TempoEvents.Keys.ToList();
        var tempoTimeSecondEvents = TempoEvents.Values.Select(x => x.Item2).ToList();

        // Attempt a binary search for the current timestamp, 
        // which will return a bitwise complement of the index of the next highest timesecond value 
        // OR tempoTimeSecondEvents.Count if there are no more elements
        var index = tempoTimeSecondEvents.BinarySearch((float)currentTimestamp);
        if (index <= 0) // bitwise complement is negative or zero
        {
            // modify index if the found timestamp is at the end of the array (last tempo event)
            if (~index == tempoTimeSecondEvents.Count) index = tempoTimeSecondEvents.Count - 1;
            // else just get the index proper 
            else index = ~index - 1; // -1 because ~index is the next timestamp AFTER the start of the window, but we need the one before to properly render beatlines
            try
            {
                return tempoTickTimeEvents[index]; 
            }
            catch
            {
                return tempoTickTimeEvents[0]; // if ~index - 1 is -1, then the index should be itself
            }
        }
        else 
        {
            // on the off-chance that someone scrolls to a point 
            // where the start of the segment IS a beatline, 
            // the binary search will return an actual index, 
            // which needs to be taken at face value
            return tempoTickTimeEvents[index];
        }
    }
}
