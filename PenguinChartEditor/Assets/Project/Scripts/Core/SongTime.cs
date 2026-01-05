using System;
using UnityEngine;

public class SongTime : MonoBehaviour
{
    private const int MIDDLE_MOUSE_BUTTON_ID = 2;
    static InputMap inputMap;

    // Needed for delta calculations when scrolling using MMB
    private float initialMouseY = float.NaN;

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
            if (value < 0) value = 0;
            if (value >= AudioManager.SongLength)
            {
                value = AudioManager.SongLength;
                AudioManager.PauseAudio();
            }

            value = Math.Round(value, 3); // So that CurrentWFDataPosition comes out clean

            if (_songPos == value) return;
            _songPos = value;

            Waveform.GenerateWaveformPoints();
            TimeChanged?.Invoke();
        }
    }
    private static double _songPos = 0;

    public static int SongPositionTicks => Waveform.songPositionTicks;

    /// <summary>
    /// The length of the song in tick time.
    /// </summary>
    public static int SongLengthTicks => Chart.SyncTrackInstrument.ConvertSecondsToTickTime(AudioManager.SongLength);

    public delegate void TimeChangedDelegate();
    public delegate void PositiveTimeChangeDelegate();
    public delegate void NegativeTimeChangeDelegate();
    public static event TimeChangedDelegate TimeChanged;
    public static event PositiveTimeChangeDelegate PositiveTimeChange;
    public static event NegativeTimeChangeDelegate NegativeTimeChange;

    #endregion

    #region Unity Functions

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.ScrollTrack.performed += scrollChange => ChangeTime(scrollChange.ReadValue<float>());

        inputMap.Charting.MiddleMouseClick.started += x => initialMouseY = Input.mousePosition.y;
        inputMap.Charting.MiddleMouseClick.canceled += x => initialMouseY = float.NaN;
    }

    void Start()
    {
        Waveform.GenerateWaveformPoints();
        TimeChanged?.Invoke();
    }

    void Update()
    {
        if (Input.GetMouseButton(MIDDLE_MOUSE_BUTTON_ID))
        {
            ChangeTime(Input.mousePosition.y - initialMouseY, middleClick: true);
        }

        // No funky calculations needed, just update the song position every frame
        // Add calibration here later on
        if (AudioManager.AudioPlaying)
        {
            SongPositionSeconds = AudioManager.GetCurrentAudioPosition();
            TimeChanged?.Invoke();
            PositiveTimeChange?.Invoke();
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
        if (scrollChange > 0) PositiveTimeChange?.Invoke();
        else NegativeTimeChange?.Invoke();
    }

    public static int CalculateGridSnappedTick(float percentOfHighway)
    {
        var cursorTimestamp = (percentOfHighway * Waveform.timeShown) + Waveform.startTime;
        var cursorTickTime = Chart.SyncTrackInstrument.ConvertSecondsToTickTime((float)cursorTimestamp);

        if (cursorTickTime < 0) return 0;

        // Calculate the Tick grid to snap the event to
        var tickInterval = Chart.Resolution / ((float)DivisionChanger.CurrentDivision / 4);

        // Calculate the cursor's Tick position in the context of the origin of the grid (last barline) 
        var divisionBasisTick = cursorTickTime - Chart.SyncTrackInstrument.GetLastBarline(cursorTickTime);

        // Find how many Ticks off the cursor position is from the grid 
        var remainder = divisionBasisTick % tickInterval;

        // Remainder will show how many Ticks off from the last event we are
        // Use remainder to determine which grid snap we are closest to and round to that
        if (remainder > (tickInterval / 2))
        {
            // Regress to last grid snap and then add a snap to get to next grid position
            var targetSnap = (int)Math.Ceiling(cursorTickTime - remainder + tickInterval);

            return Mathf.Min(targetSnap, SongLengthTicks);
        }
        else // Closer to previous grid snap or dead on a snap (subtract 0 = no change)
        {
            // Regress to last grid snap
            return (int)Math.Ceiling(cursorTickTime - remainder);
        }
    }

    #endregion
}