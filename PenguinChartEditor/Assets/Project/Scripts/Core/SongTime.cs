using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SongTime : MonoBehaviour
{
    private const int MIDDLE_MOUSE_BUTTON_ID = 2;
    private const float MINUTES_TO_SECONDS_CONVERSION = 60;
    private const float MILLISECONDS_TO_SECONDS_CONVERSION = 1000;
    
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

    public static int SongPositionTicks
    {
        get => Waveform.songPositionTicks;
        set
        {
            SongPositionSeconds = Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(value);
        }
    }

    /// <summary>
    /// The length of the song in tick time.
    /// </summary>
    // FIXME/TODO: Cache this on chart load so that this expensive method isn't called so much
    public static int SongLengthTicks => Chart.SyncTrackInstrument.ConvertSecondsToTickTime(SongLength);

    public static float SongLength => (float)AudioManager.SongLength;

    public delegate void TimeChangedDelegate();
    public delegate void PositiveTimeChangeDelegate();
    public delegate void NegativeTimeChangeDelegate();
    public static event TimeChangedDelegate TimeChanged;
    public static event PositiveTimeChangeDelegate PositiveTimeChange;
    public static event NegativeTimeChangeDelegate NegativeTimeChange;

    #endregion

    #region Unity Functions

    private void Start()
    {
        Chart.instance.inputMap.StandardStaticEvents.ScrollTrack.performed += ChangeTime;
        Chart.instance.inputMap.StandardStaticEvents.MiddleMouseClick.started += UpdateInitialMouseY;
        Chart.instance.inputMap.StandardStaticEvents.MiddleMouseClick.canceled += ResetInitialMouseY;
        
        Waveform.GenerateWaveformPoints();
        TimeChanged?.Invoke();
        Chart.InPlaceRefresh();
    }

    private void UpdateInitialMouseY(InputAction.CallbackContext _) => initialMouseY = Input.mousePosition.y;
    private void ResetInitialMouseY(InputAction.CallbackContext _) => initialMouseY = float.NaN;

    private void OnDestroy()
    {
        Chart.instance.inputMap.StandardStaticEvents.ScrollTrack.performed -= ChangeTime;
        Chart.instance.inputMap.StandardStaticEvents.MiddleMouseClick.started -= UpdateInitialMouseY;
        Chart.instance.inputMap.StandardStaticEvents.MiddleMouseClick.canceled -= ResetInitialMouseY;
    }

    public static void StopPlaybackAndTimeEditActions()
    {
        Chart.instance.inputMap.StandardStaticEvents.Disable();
        AudioManager.DisableAudioPlaybackControls();
    }

    public static void AllowPlaybackAndTimeEditActions()
    {
        Chart.instance.inputMap.StandardStaticEvents.Enable();
        AudioManager.EnableAudioPlaybackControls();
    }

    private void Update()
    {
        if (Input.GetMouseButton(MIDDLE_MOUSE_BUTTON_ID))
        {
            ChangeTime(Input.mousePosition.y - initialMouseY, middleClick: true);
        }

        // No funky calculations needed, just update the song position every frame
        // Add calibration here later on
        if (AudioManager.AudioPlaying)
        {
            SongPositionSeconds = AudioManager.AudioPosition;
            TimeChanged?.Invoke();
            PositiveTimeChange?.Invoke();
        }
    }

    #endregion

    #region Time Modification

    private static void ChangeTime(InputAction.CallbackContext context) => ChangeTime(context.ReadValue<float>());

    /// <summary>
    /// Change the timestamp of the song from a specified scroll change.
    /// </summary>
    /// <param name="scrollChange"></param>
    /// <param name="middleClick"></param>
    public static void ChangeTime(float scrollChange, bool middleClick = false)
    {
        if (AudioManager.AudioPlaying || float.IsNaN(scrollChange) || scrollChange == 0) return; // for some reason when the input map is reenabled it passes NaN into this function so we will be having none of that thank you 

        // If it's a middle click, the delta value is wayyy too large so this is a solution FOR NOW
        var scrollSuppressant = 1;
        if (middleClick) scrollSuppressant = 50;
        var newTimeCandidate = SongPositionSeconds + scrollChange / (Chart.settings.ScrollSensitivity * scrollSuppressant);

        // Clamp position to within the length of the song
        if (newTimeCandidate < 0)
        {
            newTimeCandidate = 0;
        }
        else if (newTimeCandidate >= SongLength)
        {
            newTimeCandidate = SongLength;
        }

        SongPositionSeconds = newTimeCandidate;
        if (scrollChange > 0) PositiveTimeChange?.Invoke();
        else NegativeTimeChange?.Invoke();
    }

    public static int CalculateCurrentMouseTick() =>
        CalculateGridSnappedTick(Chart.GetCursorHighwayProportion());

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

    public static void UpdateSongTimestampFromFormattedTimestamp(string timestamp) =>
        SongPositionSeconds = ConvertFormattedTimestampToSeconds(timestamp);
    public static float ConvertFormattedTimestampToSeconds(string timestamp)
    {
        int minutes = 0;
        int seconds = 0;
        int milliseconds = 0;

        bool noSplit;

        // Isolate minute value, if it exists
        try
        {
            var minSplit = timestamp.Split(':');
            minutes = int.Parse(minSplit[0]);
            timestamp = minSplit[1];
        }
        catch { noSplit = true; minutes = 0; } // minutes is set to zero to prevent doubling values when minutes is set to the first array val

        // Isolate second and millisecond value, if it exists
        try
        {
            var secSplit = timestamp.Split('.');
            seconds = int.Parse(secSplit[0]);
            milliseconds = int.Parse(secSplit[1]);

            noSplit = false;
        }
        catch { noSplit = true; }

        // If no other time type/divider is present, interpret the raw number as seconds
        if (noSplit) seconds = int.Parse(timestamp);

        // Convert and add together isolated values
        return 
            minutes * MINUTES_TO_SECONDS_CONVERSION + 
            seconds + 
            milliseconds / MILLISECONDS_TO_SECONDS_CONVERSION;
    }

    /// <summary>
    /// Take a number of seconds (in S.ms form - ex. 61.1 seconds) and convert it to MM:SS.mmm format (where 61.1 returns 01:01.100)
    /// </summary>
    /// <param name="position">The unformatted second count.</param>
    /// <param name="includeMS">Include milliseconds portion? Truncates if false.</param>
    /// <returns>The formatted MM:SS:mmm timestamp of the second position</returns>
    public static string ConvertSecondsToTimestamp(double position, bool includeMS = true)
    {
        var minutes = Math.Floor(position / 60);
        var secondsWithMS = position - minutes * 60;
        var seconds = (int)Math.Floor(secondsWithMS);
        var milliseconds = Math.Round(secondsWithMS - seconds, 3) * 1000;

        string minutesString = minutes.ToString();
        if (minutes < 10)
        {
            minutesString = minutesString.PadLeft(minutesString.Length + 1, '0');
        }

        string secondsString = seconds.ToString();
        if (seconds < 10)
        {
            secondsString = secondsString.PadLeft(2, '0');
        }

        string millisecondsString = milliseconds.ToString();
        if (millisecondsString.Length < 3)
        {
            millisecondsString = millisecondsString.PadRight(3, '0');
        }

        var msPortion = includeMS ? "." + millisecondsString : "";
        return minutesString + ":" + secondsString + msPortion;
    }

    #endregion
}