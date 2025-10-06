using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class TempoManager : MonoBehaviour
{
    static RectTransform boundaryReference;
    void Awake()
    {
        // set up events so that beatlines can update whenever anything changes
        Waveform.DisplayChanged += UpdateBeatlines;
        boundaryReference = GameObject.Find("ScreenReference").GetComponent<RectTransform>();
        Chart.currentTab = Chart.TabType.TempoMap;
    }
    
    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    public static void UpdateBeatlines()
    {
        //Debug.Log($"Generating beatlines. {Time.frameCount}");
        if (Chart.currentTab != Chart.TabType.TempoMap)
            throw new System.Exception($"TempoManager.UpdateBeatlines is for use only in the TempoMap scene. Please call the correct scene refresh for {Chart.currentTab}.");
            
        int currentBeatline = 0;
        // Generate the division and half-division beatlines
        for (
                int currentTick = TimeSignature.GetNextBeatlineEvent(Waveform.startTick); // Calculate the tick to start generating beatlines from
                currentTick < Waveform.endTick && // Don't generate beatlines outside of the shown time period
                currentTick < SongTimelineManager.SongLengthTicks; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            Debug.Log($"{Time.frameCount} New beatline created. This tick: {currentTick}. Positioning: ({BPM.ConvertTickTimeToSeconds(currentTick)} - {Waveform.startTime}) => {(BPM.ConvertTickTimeToSeconds(currentTick) - Waveform.startTime)} / {Waveform.timeShown}, {boundaryReference.rect.height}");
            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.Tick = currentTick;

            workedBeatline.CheckForEvents();

            workedBeatline.UpdateBeatlinePosition((BPM.ConvertTickTimeToSeconds(currentTick) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);

            // Needed to generate correct thickness
            workedBeatline.Type = TimeSignature.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += TimeSignature.IncreaseByHalfDivision(currentTick);
        }

        // Get list of tempo events that *should* be displayed during the visible window  
        var ignoredKeys = BPM.EventData.Events.Keys.Where(key => key >= Waveform.startTick && key <= Waveform.endTick && key % TimeSignature.IncreaseByHalfDivision(key) != 0).ToHashSet();
        var ignoredTSKeys = TimeSignature.EventData.Events.Keys.Where(key => key >= Waveform.startTick && key <= Waveform.endTick && key % TimeSignature.IncreaseByHalfDivision(key) != 0).ToHashSet();

        ignoredKeys.UnionWith(ignoredTSKeys);

        foreach (var tick in ignoredKeys)
        {
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.Tick = tick;

            workedBeatline.CheckForEvents();

            workedBeatline.UpdateBeatlinePosition((BPM.ConvertTickTimeToSeconds(tick) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);

            workedBeatline.Type = Beatline.BeatlineType.none;

            currentBeatline++;
        }

        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);
    }
}
