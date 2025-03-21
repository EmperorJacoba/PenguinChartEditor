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

        HashSet<int> recognizedTempoChanges = new(); // Needed for irregular/out of step tempo changes
        HashSet<int> recognizedTSChanges = new();
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
                workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[currentTick].Item1.ToString();
                recognizedTempoChanges.Add(currentTick);
            }
            else
            {
                workedBeatline.BPMLabelVisible = false;
            }

            if (SongTimelineManager.TimeSignatureEvents.ContainsKey(currentTick))
            {
                workedBeatline.TSLabelText = $"{SongTimelineManager.TimeSignatureEvents[currentTick].Item1} / {SongTimelineManager.TimeSignatureEvents[currentTick].Item2}";
                recognizedTSChanges.Add(currentTick);
            }
            else
            {
                workedBeatline.TSLabelVisible = false;
            }

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            // Needed to generate correct thickness
            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += (int)(SongTimelineManager.PLACEHOLDER_RESOLUTION / SongTimelineManager.CalculateDivision(currentTick) / 2);
        }

        // Get list of tempo events that *should* be displayed during the visible window  
        var validTempoKeys = SongTimelineManager.TempoEvents.Keys.Where(key => key >= startTick && key <= endTick).ToList();

        // Find all of the tempo events not already accounted for and add a BPM label for it
        for (int i = 0; i <= validTempoKeys.Count - 1; i++)
        {
            if (!recognizedTempoChanges.Contains(validTempoKeys[i]))
            {
                var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

                workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[validTempoKeys[i]].Item1.ToString();

                workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(validTempoKeys[i]) - startTime)/timeShown); 

                workedBeatline.line.enabled = false; // The line will show sometimes if this is not here specifically
                
                currentBeatline++;
            }
        }

        var validTSKeys = SongTimelineManager.TimeSignatureEvents.Keys.Where(key => key >= startTick && key <= endTick).ToList();
        for (int i = 0; i <= validTSKeys.Count - 1; i++)
        {
            if (!recognizedTempoChanges.Contains(validTSKeys[i]))
            {
                var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

                workedBeatline.TSLabelText = $"{SongTimelineManager.TimeSignatureEvents[validTSKeys[i]].Item1} / {SongTimelineManager.TimeSignatureEvents[validTSKeys[i]].Item2}";

                workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(validTSKeys[i]) - startTime)/timeShown);

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
