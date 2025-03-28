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

            workedBeatline.CheckForEvents(currentTick);

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            // Needed to generate correct thickness
            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += IncreaseByHalfDivision(currentTick);
        }

        // Get list of tempo events that *should* be displayed during the visible window  
        var ignoredKeys = SongTimelineManager.TempoEvents.Keys.Where(key => key >= startTick && key <= endTick && key % IncreaseByHalfDivision(key) != 0).ToHashSet();
        var ignoredTSKeys = SongTimelineManager.TimeSignatureEvents.Keys.Where(key => key >= startTick && key <= endTick && key % IncreaseByHalfDivision(key) != 0).ToHashSet();

        ignoredKeys.UnionWith(ignoredTSKeys);

        foreach (var tick in ignoredKeys)
        {
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.CheckForEvents(tick);

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(tick) - startTime)/timeShown); 

            workedBeatline.Type = Beatline.BeatlineType.none;
            currentBeatline++;
        }

        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);
    }

    int IncreaseByHalfDivision(int tick)
    {
        return (int)(ChartMetadata.ChartResolution / SongTimelineManager.CalculateDivision(tick) / 2);
    }

    // Implement moving beatlines and actually tempo mapping
        // They move only in Y-direction -> X-dir is locked

    // Add pre-rendered beatlines/line rendering

    // Next steps:
    // Add chart resolution parsing
    // Add time signature labels
        // Test more time signature stuff (use Yes songs)
    // Label bar numbers
    // Add editing functionality for beatlines and time signatures
    // Add volume changing
    // Add speed changing

}
