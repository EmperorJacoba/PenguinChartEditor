using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TempoManager : MonoBehaviour
{  
    private WaveformManager waveformManager;

    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();

        SongTimelineManager.TimeChanged += UpdateBeatlines; // set up event so that beatlines can update properly
        WaveformManager.DisplayChanged += UpdateBeatlines;
    }

    void Start()
    {
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

        HashSet<int> recognizedChanges = new();
        // Set up different iterators
        int currentBeatline = 0; // Holds which beatline is being modified at the present moment
        // Actually generate beatlines (currently basic quarter note math atm)
        for (
                int currentTick = SongTimelineManager.CalculateNextBeatlineEvent(startTick); // Calculate the timestamp to start generating beatlines from (GetStartingTimestamp)
                currentTick < endTick && // Don't generate beatlines outside of the shown time period
                currentTick < SongTimelineManager.SongLengthTicks; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

            workedBeatline.BPMLabelVisible = true;
            workedBeatline.BPMLabelText = currentTick.ToString();
            if (SongTimelineManager.TempoEvents.ContainsKey(currentTick))
            {

                // workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[currentTick].Item1.ToString();
                recognizedChanges.Add(currentTick);
            }
            else
            {
                //workedBeatline.BPMLabelVisible = false;
            }

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);

            currentTick += (int)(SongTimelineManager.PLACEHOLDER_RESOLUTION / SongTimelineManager.CalculateDivision(currentTick) / 2);
        }

        var validKeys = SongTimelineManager.TempoEvents.Keys.Where(key => key >= startTick && key <= endTick).ToList();
        for (int i = 0; i <= validKeys.Count - 1; i++)
        {
            if (!recognizedChanges.Contains(validKeys[i]))
            {
                var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);

                workedBeatline.BPMLabelVisible = true;
                workedBeatline.BPMLabelText = validKeys[i].ToString();
                //workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[validKeys[i]].Item1.ToString();

                workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(validKeys[i]) - startTime)/timeShown); 
                workedBeatline.Type = Beatline.BeatlineType.none;
                workedBeatline.line.enabled = false; // The line will show sometimes if this is not here specifically
                currentBeatline++;
            }
        }

        // CURRENT ISSUE:

        // Beatlines are not rendering properly when presented with improper tempo change

        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);


        // Sweep for special labels (irregular beatline label placement) here
    }
    // 192 / 4 = 48 = sixteenth note
    // 192 / 2 = 96 = eighth note
    // 192 / 1 = 192 = quarter note
    // 192 / 0.5 = 384 = half note
    // 192 / 0.25 = 768 = whole note

    // Use existing .chart data to improve beatline algorithm and modify as needed
    // Implement moving beatlines and actually tempo mapping
        // They move only in Y-direction -> X-dir is locked
}
