using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TempoManager : MonoBehaviour
{    
    /// <summary>
    /// Dictionary that contains time signature changes and corresponding tick time positions.
    /// <para>Key = Tick-time position. Value = Numerator (num of beats per bar), Denominator (type of beat)</para>
    /// <para>Example: 192 = 4, 4</para>
    /// <remarks>When writing to file, take the base 2 logarithm of the denominator to get proper .chart format. (where example would show as 192 = TS 4 2)</remarks>
    /// </summary>
    public static SortedDictionary<int, (int, int)> TimeSignatureEvents {get; set;}

    /// <summary>
    /// The thickness of a bar starting line
    /// </summary>
    private float barLineThickness = 0.05f;

    /// <summary>
    /// The thickness of the division line (e.g quarter note in 4/4 or eighth note in 7/8)
    /// </summary>;
    private float divisionLineThickness = 0.03f;

    /// <summary>
    /// The thickness of the second division line (e.g eighth note in 4/4)
    /// </summary>
    private float halfDivisionLineThickness = 0.01f;

    private WaveformManager waveformManager;
    private Strikeline strikeline;
    
    public const int PLACEHOLDER_RESOLUTION = 320;

    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();
        strikeline = GameObject.Find("Strikeline").GetComponent<Strikeline>();

        SongTimelineManager.TimeChanged += SongTimeChanged; // set up event so that beatlines can update properly
        WaveformManager.DisplayChanged += SongTimeChanged;
    }

    void Start()
    {

        SongTimeChanged(); // render beatlines for first time
    }

    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    void SongTimeChanged()
    {
        // Get the period of time shown on screen and the amount of time shown for position and bounds calculations 
        (var startTime, var endTime) = waveformManager.GetDisplayedAudio();
        var timeShown = endTime - startTime;

        // Get a list of all beat changes in the TempoEvents dict that are present in the given time interval to get basis for calculating beatlines
        List<int> validTempoEvents = TempoEvents.Where(n => n.Value.Item2 >= startTime && n.Value.Item2 <= endTime).ToDictionary(item => item.Key, item => item.Value).Keys.ToList();

        // Because tempo events are not guaranteed to be the first beatline in a given time period, 
        // find the last tempo event before the time period to generate first beatlines
        validTempoEvents.Insert(0, FindPrecedingTempoEvent(startTime));

        // Set up different iterators
        int currentBeatline = 0; // Holds which beatline is being modified at the present moment
        int validEventIndex = 0; // Holds the tempo event from validTempoEvents that is being used to calculate new positions
        // Actually generate beatlines (currently basic quarter note math atm)
        for (
                float currentTimestamp = GetStartingTimestamp(validTempoEvents[0], (float)startTime); // Calculate the timestamp to start generating beatlines from (GetStartingTimestamp)
                currentTimestamp < endTime && // Don't generate beatlines outside of the shown time period
                currentTimestamp < PluginBassManager.SongLength; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

            // Timestamp is calculated before loop starts, so start by updating the selected beatline's position
            workedBeatline.UpdateBeatlinePosition((currentTimestamp - startTime)/timeShown); 
            if (currentTimestamp == TempoEvents[validTempoEvents[validEventIndex]].Item2)
            {
                workedBeatline.BPMLabelVisible = true;
                workedBeatline.BPMLabelText = $"{TempoEvents[validTempoEvents[validEventIndex]].Item1}";
            }
            else
            {
                workedBeatline.BPMLabelVisible = false;
            }

            // Calculate what the next beatline timestamp will be based on the current BPM
            currentTimestamp += 60 / TempoEvents[validTempoEvents[validEventIndex]].Item1;
            try 
            {
                // If we've passed or hit the calculated position of another tempo change,
                // Set the new beatline position to the position of that tempo event and generate beatlines with new BPM
                if (currentTimestamp >= TempoEvents[validTempoEvents[validEventIndex + 1]].Item2)
                {
                    currentTimestamp = TempoEvents[validTempoEvents[validEventIndex + 1]].Item2;

                    validEventIndex++;
                }
                // Current notes:
                // Only partially accounts for anything smaller than a quarter note (gets new BPM, but acts as if the next note is a quarter note)
                // Should calculate notes a quarter note away for BPM regardless of other factors (b/c time signature changes could reset which notes are major beats)
            }
            catch
            {
                continue; // Reached last valid tempo event, so just continue through loop without checking for more
                // (This results from an index out of bounds error)
            }
        }
        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);

        // take the first beatline in the valid timestamps -> generate that beatline, beatline & label visible
        // get distance between beatlines based on paired BPM, make next beatlines based on that until you hit next timestamp -> beatline visible but label invisible
            // at that timestamp, recalculate distance to the next beatline
            // this can also happen in the middle of two beatlines -> if this happens, keep the label, hide the beatline
                // find distance between current division and main division, and then render bpm normally
                // example: 0 -> 192 -> 384 -> 576 -> 768 is standard quarter note tick-timestamps
                // if one falls on tick 64, you need to render where the 192 beatline would be if BPM was on tick 0 
                // this logic can fit in for all beatlines
    }

    // Time signatures
    // Get the last time signature
    // # of beats in a bar is numerator multiplied by the chart resolution 
    // If current beat tick-time - tick-time of last time sig % chartres * num * (denom / 4) == 0, then we've hit a new bar
        // If this is not true for a TS with its last TS, there's a TS error
        // Notify the user
    // Check for new time signature
        // If there is a time signature on the current beatline tick-time timestamp, that beatline is the first note of the bar and gets the bar line thickness
        // OR if it satisfies the modulo above, it gets the bar line thickness
            // If not true, we're on a secondary beat
            // Check to see if it's a first-div
                // current beat tick time - tick time of last TS event % chartres * (denom / 4)
            // If not, check for second-div
                // current beat tick time - tick time of last TS event % chartres * (denom / 8)

    Beatline.BeatlineType CalculateBeatlineThickness(int beatlineTickTimePos, int lastTSTickTimePos)
    {
        var tsDiff = beatlineTickTimePos - lastTSTickTimePos;
        if (tsDiff % (PLACEHOLDER_RESOLUTION * TimeSignatureEvents[lastTSTickTimePos].Item1 * TimeSignatureEvents[lastTSTickTimePos].Item2) == 0)
        {
            return Beatline.BeatlineType.barline;
        }
        else if (tsDiff % (PLACEHOLDER_RESOLUTION * (TimeSignatureEvents[lastTSTickTimePos].Item2 / 4)) == 0)
        {
            return Beatline.BeatlineType.divisionLine;
        }
        else if (tsDiff % (PLACEHOLDER_RESOLUTION * (TimeSignatureEvents[lastTSTickTimePos].Item2 / 8)) == 0)
        {
            return Beatline.BeatlineType.halfDivisionLine;
        }
        return Beatline.BeatlineType.none;
    }
    
    // 192 / 4 = 48 = sixteenth note
    // 192 / 2 = 96 = eighth note
    // 192 / 1 = 192 = quarter note
    // 192 / 0.5 = 384 = half note
    // 192 / 0.25 = 768 = whole note

    /// <summary>
    /// Get the first beatline timestamp that exists after a given timestamp.
    /// </summary>
    /// <param name="tickTimeEvent">The tick-event to start calculating from.</param>
    /// <param name="startTime">The comparison timestamp.</param>
    /// <returns>The time-second position of the beatline.</returns>
    private float GetStartingTimestamp(int tickTimeEvent, float startTime)
    {
        // Get vals
        var bpm = TempoEvents[tickTimeEvent].Item1;
        var tempoEventStartPoint = TempoEvents[tickTimeEvent].Item2;

        // Event is already in the window, so no need to do any calculations
        if (tempoEventStartPoint >= startTime) return tempoEventStartPoint;

        // Find number of beats from the tempo start point to the start of the window
        var timeDiff = startTime - tempoEventStartPoint;
        var beatInterval = 60 / bpm;
        var numIntervals = (int)(timeDiff / beatInterval);

        // Convert intervals to seconds and add to the start point to get the first quarter note in the window
        var nextBeatlineTimestamp = tempoEventStartPoint + (numIntervals + 1) * beatInterval;
        return nextBeatlineTimestamp;
    }



    // Next steps:
    // Make finding valid tempo events more efficient
    // Make simple Beatline functions into properties
        // Maybe also add some debug properties like tick-time event? Auto implemented or hardcoded? etc


    // Implement importing of existing .chart data (Tempo events only)\
        // Use an existing SyncTrack to generate beatlines
        // Read a .chart file
        // Get [SyncTrack] section
        // Parse section data into TempoEvents
        // Calculate beatline positions from new data
        // Render beatlines


    // Use existing .chart data to improve beatline algorithm and modify as needed
    // Implement moving beatlines and actually tempo mapping
        // They move only in Y-direction -> X-dir is locked
}
