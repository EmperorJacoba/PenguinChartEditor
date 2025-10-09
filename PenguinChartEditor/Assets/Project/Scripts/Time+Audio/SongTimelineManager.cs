using System;
using UnityEngine;

public class SongTimelineManager : MonoBehaviour
{
    static InputMap inputMap;

    // Needed for delta calculations when scrolling using MMB
    private float initialMouseY = float.NaN;
    private float currentMouseY;

    #region Properties

    /// <summary>
    /// The current timestamp of the song at the strikeline.
    /// </summary>
    public static double SongPositionSeconds
    {
        get
        {
            return _songPos;
        }
        set
        {
            if (value > AudioManager.SongLength) return;
            value = Math.Round(value, 3); // So that CurrentWFDataPosition comes out clean
            if (_songPos == value) return;
            _songPos = value;

            if (_songPos < 0) throw new ArgumentException();

            TimeChanged?.Invoke();
        }
    }

    public static int SongPositionTicks
    {
        get
        {
            return Tempo.ConvertSecondsToTickTime((float)_songPos);
        }
    }
    private static double _songPos = 0;

    public delegate void TimeChangedDelegate();

    /// <summary>
    /// Event that fires whenever the song position changes.
    /// </summary>
    public static event TimeChangedDelegate TimeChanged;

    /// <summary>
    /// The length of the song in tick time.
    /// </summary>
    public static int SongLengthTicks => Tempo.ConvertSecondsToTickTime(AudioManager.SongLength);

    public static void InvokeTimeChanged() => TimeChanged?.Invoke();

    #endregion

    #region Unity Functions
    void Awake()
    {
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
        TimeChanged?.Invoke();
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
        if (AudioManager.AudioPlaying)
        {
            SongPositionSeconds = AudioManager.GetCurrentAudioPosition();
        }
    }

    public static void ToggleChartingInputMap()
    {
        if (inputMap.Charting.enabled) inputMap.Charting.Disable();
        else inputMap.Charting.Enable();
    }

    public static void DisableChartingInputMap()
    {
        inputMap.Charting.Disable();
    }

    public static void EnableChartingInputMap()
    {
        inputMap.Charting.Enable();
    }

    #endregion

    #region Time Modification

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
    public static void ChangeTime(float scrollChange, bool middleClick = false)
    {
        if (float.IsNaN(scrollChange)) return; // for some reason when the input map is reenabled it passes NaN into this function so we will be having none of that thank you 

        double newTimeCandidate;

        // If it's a middle click, the delta value is wayyy too large so this is a solution FOR NOW
        var scrollSuppressant = 1;
        if (middleClick) scrollSuppressant = 50;
        newTimeCandidate = SongPositionSeconds + scrollChange / (UserSettings.ScrollSensitivity * scrollSuppressant);

        // Clamp position to within the length of the song
        if (newTimeCandidate < 0)
        {
            newTimeCandidate = 0;
        }
        else if (newTimeCandidate >= AudioManager.SongLength)
        {
            newTimeCandidate = AudioManager.SongLength;
        }

        SongPositionSeconds = newTimeCandidate;
    }
    
    public static int CalculateGridSnappedTick(float percentOfScreenVertical)
    {
        var cursorTimestamp = (percentOfScreenVertical * Waveform.timeShown) + Waveform.startTime;
        var cursorTickTime = Tempo.ConvertSecondsToTickTime((float)cursorTimestamp);

        if (cursorTickTime < 0) return 0;

        // Calculate the Tick grid to snap the event to
        var tickInterval = Chart.Resolution / ((float)DivisionChanger.CurrentDivision / 4);

        // Calculate the cursor's Tick position in the context of the origin of the grid (last barline) 
        var divisionBasisTick = cursorTickTime - TimeSignature.GetLastBarline(cursorTickTime);

        // Find how many Ticks off the cursor position is from the grid 
        var remainder = divisionBasisTick % tickInterval;

        // Debug.Log($"cursor timestamp: {cursorTimestamp}, cursor ticktime: {cursorTickTime}, tick interval: {tickInterval}, div basis: {divisionBasisTick}, remainder: {remainder}");

        // Remainder will show how many Ticks off from the last event we are
        // Use remainder to determine which grid snap we are closest to and round to that
        if (remainder > (tickInterval / 2)) // Closer to following snap
        {
            // Regress to last grid snap and then add a snap to get to next grid position
            return (int)Math.Floor(cursorTickTime - remainder + tickInterval);
        }
        else // Closer to previous grid snap or dead on a snap (subtract 0 = no change)
        {
            // Regress to last grid snap
            return (int)Math.Floor(cursorTickTime - remainder);
        }
    }

    #endregion
}