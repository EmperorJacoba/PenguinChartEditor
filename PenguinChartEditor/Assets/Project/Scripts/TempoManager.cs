using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TempoManager : MonoBehaviour
{    
    /// <summary>
    /// Dictionary that contains tempo changes and corresponding tick time positions. 
    /// <para> Key = Tick-time position. Value = BPM to three decimal places, time-second value of the tempo change. </para>
    /// <para>Example: 192 = 102.201, 0.237</para>
    /// <remarks>When writing to file, multiply BPM value by 100 to get proper .chart format (where example would show as B 192 = 102201)</remarks>
    /// </summary>
    public SortedDictionary<int, (float, float)> TempoEvents {get; set;} // This is sorted dict so that it is easier to read & write tempo changes

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
    

    void Awake()
    {
        TempoEvents = new();
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();
        strikeline = GameObject.Find("Strikeline").GetComponent<Strikeline>();
        WaveformManager.WFPositionChanged += WaveformChanged; // set up event so that beatlines can update properly
    }

    void Start()
    {
        TempoEvents = ChartParser.GetTempoEventDict("C:/_PCE_files/TestAudioFiles/Burning.chart");
        if (TempoEvents.Count == 0) // if there is no data to load in 
        {
            TempoEvents.Add(0, (120.0f, 0)); // add placeholder bpm
        }
        WaveformChanged(); // render beatlines for first time
    }

    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    void WaveformChanged()
    {
        // Get the period of time shown on screen and the amount of time shown for position and bounds calculations 
        (var startTime, var endTime) = waveformManager.GetDisplayedAudioPositions();
        var timeShown = endTime - startTime;

        // Get a list of all beat changes in the TempoEvents dict that are present in the given time interval to get basis for calculating beatlines
        // THERE IS ABSOLUTELY A MORE EFFICIENT WAY TO DO THIS => UPDATE LATER
        List<int> validTempoEvents = TempoEvents.Where(n => n.Value.Item2 >= startTime && n.Value.Item2 <= endTime).ToDictionary(item => item.Key, item => item.Value).Keys.ToList();

        // Because tempo events are not guaranteed to be the first beatline in a given time period, 
        // find the last tempo event before the time period to generate first beatlines
        validTempoEvents.Insert(0, FindPrecedingTempoEvent(startTime));

        // Set up different iterators
        int currentBeatline = 0; // Holds which beatline is being modified at the present moment
        int validEventIndex = 1; // Holds the tempo event from validTempoEvents that is being used to calculate new positions

        // Actually generate beatlines (currently basic quarter note math atm)
        for (
                float currentTimestamp = GetStartingTimestamp(validTempoEvents[0], startTime); // Calculate the timestamp to start generating beatlines from (GetStartingTimestamp)
                currentTimestamp < endTime && // Don't generate beatlines outside of the shown time period
                currentTimestamp < PluginBassManager.SongLength; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {

            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.UpdateBPMLabelText(currentBeatline);

            // Timestamp is calculated before loop starts, so start by updating the selected beatline's position
            workedBeatline.UpdateBeatlinePosition((currentTimestamp - startTime)/timeShown); 

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
                // Not actually tested yet
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

    /// <summary>
    /// Get the first beatline timestamp that exists after a given timestamp.
    /// </summary>
    /// <param name="tickTimeEvent">The tick-event to start calculating from.</param>
    /// <param name="startTime">The comparison timestamp.</param>
    /// <returns>The time-second position of the beatline.</returns>
    private float GetStartingTimestamp(int tickTimeEvent, float startTime)
    {
        var bpm = TempoEvents[tickTimeEvent].Item1;
        var tempoEventStartPoint = TempoEvents[tickTimeEvent].Item2;
        while (tempoEventStartPoint < startTime)
        {
            tempoEventStartPoint += 60 / bpm;
        }
        return tempoEventStartPoint;
    }

    /// <summary>
    /// Get the tick-time event directly before a given timestamp from TempoEvents.
    /// </summary>
    /// <param name="currentTimestamp">The timestamp to get the tick-time event from.</param>
    /// <returns>The tick-time event before currentTimestamp in TempoEvents</returns>
    private int FindPrecedingTempoEvent(float currentTimestamp)
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
        var index = tempoTimeSecondEvents.BinarySearch(currentTimestamp);
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






    // Current issues:
    // Beatlines do not generate properly from a valid tempo dictionary made by ChartParser

    
    // Next steps:
    // Make finding valid tempo events more efficient
    // Fix normalization so that amplitude can be changed
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
