using System.Linq;
using UnityEngine;

public class TempoManager : MonoBehaviour
{  
    private static WaveformManager waveformManager;

    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();

        // set up events so that beatlines can update whenever anything changes
        WaveformManager.DisplayChanged += UpdateBeatlines;
    }
    
    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    public static void UpdateBeatlines()
    {
        waveformManager.GetCurrentDisplayedWaveformInfo(out var startTick, out var endTick, out var timeShown, out var startTime, out var endTime);

        int currentBeatline = 0;
        // Generate the division and half-division beatlines
        for (
                int currentTick = SongTimelineManager.FindNextBeatlineEvent(startTick); // Calculate the tick to start generating beatlines from
                currentTick < endTick && // Don't generate beatlines outside of the shown time period
                currentTick < SongTimelineManager.SongLengthTicks; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.HeldTick = currentTick;

            workedBeatline.CheckForEvents();

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            // Needed to generate correct thickness
            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += SongTimelineManager.IncreaseByHalfDivision(currentTick);
        }

        // Get list of tempo events that *should* be displayed during the visible window  
        var ignoredKeys = SongTimelineManager.TempoEvents.Keys.Where(key => key >= startTick && key <= endTick && key % SongTimelineManager.IncreaseByHalfDivision(key) != 0).ToHashSet();
        var ignoredTSKeys = SongTimelineManager.TimeSignatureEvents.Keys.Where(key => key >= startTick && key <= endTick && key % SongTimelineManager.IncreaseByHalfDivision(key) != 0).ToHashSet();

        ignoredKeys.UnionWith(ignoredTSKeys);

        foreach (var tick in ignoredKeys)
        {
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.HeldTick = tick;

            workedBeatline.CheckForEvents();

            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(tick) - startTime)/timeShown); 

            workedBeatline.Type = Beatline.BeatlineType.none;

            currentBeatline++;
        }

        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);
    }
}
