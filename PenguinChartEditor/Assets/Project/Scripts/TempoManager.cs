using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TempoManager : MonoBehaviour
{  
    private WaveformManager waveformManager;

    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();

        SongTimelineManager.TimeChanged += UpdateBeatlines; // set up events so that beatlines can update whenever anything changes
        WaveformManager.DisplayChanged += UpdateBeatlines;
    }
    
    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    void UpdateBeatlines()
    {
        // Get the period of time shown on screen and the amount of time shown for position and bounds calculations 
        (var startTime, var endTime) = waveformManager.GetDisplayedAudio();
        int startTick = SongTimelineManager.ConvertSecondsToTickTime((float)startTime);
        int endTick = SongTimelineManager.ConvertSecondsToTickTime((float)endTime);
        var timeShown = endTime - startTime;

        HashSet<int> recognizedChanges = new(); // Needed for irregular/out of step tempo changes
        int currentBeatline = 0;

        // Generate the division and half-division beatlines
        for (
                int currentTick = SongTimelineManager.CalculateNextBeatlineEvent(startTick); // Calculate the tick to start generating beatlines from
                currentTick < endTick && // Don't generate beatlines outside of the shown time period
                currentTick < SongTimelineManager.SongLengthTicks; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

            // If there is a tempo event on this generated beatline, make sure the label displays the BPM change
            if (SongTimelineManager.TempoEvents.ContainsKey(currentTick))
            {
                workedBeatline.BPMLabelVisible = true;
                workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[currentTick].Item1.ToString();
                recognizedChanges.Add(currentTick);
            }
            else
            {
                workedBeatline.BPMLabelVisible = false;
            }

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            // Needed to generate correct thickness
            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += (int)(SongTimelineManager.PLACEHOLDER_RESOLUTION / SongTimelineManager.CalculateDivision(currentTick) / 2);
        }

        // Get list of tempo events that *should* be displayed during the visible window  
        var validKeys = SongTimelineManager.TempoEvents.Keys.Where(key => key >= startTick && key <= endTick).ToList();

        // Find all of the tempo events not already accounted for and add a BPM label for it
        for (int i = 0; i <= validKeys.Count - 1; i++)
        {
            if (!recognizedChanges.Contains(validKeys[i]))
            {
                var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

                workedBeatline.BPMLabelVisible = true;
                workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[validKeys[i]].Item1.ToString();

                workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(validKeys[i]) - startTime)/timeShown); 

                workedBeatline.Type = Beatline.BeatlineType.none;

                workedBeatline.line.enabled = false; // The line will show sometimes if this is not here specifically
                
                currentBeatline++;
            }
        }

        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);
    }
    // Implement moving beatlines and actually tempo mapping
        // They move only in Y-direction -> X-dir is locked

    // Next steps:
    // Add chart resolution parsing
    // Add time signature labels
        // Test more time signature stuff (use Yes songs)
    // Label bar numbers
    // Add editing functionality for beatlines and time signatures
    // Add volume changing
    // Add speed changing

}
