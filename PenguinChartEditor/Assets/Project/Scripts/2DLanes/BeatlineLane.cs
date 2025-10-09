using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class BeatlineLane : MonoBehaviour
{
    [SerializeField] RectTransform boundaryReference;
    public static BeatlineLane instance;

    void Awake()
    {
        instance = this;
        Chart.currentTab = Chart.TabType.TempoMap;
    }
    
    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    public void UpdateEvents()
    {
        int currentBeatline = 0;
        // Generate the division and half-division beatlines
        for (
                int currentTick = TimeSignature.GetNextBeatlineEvent(Waveform.startTick); // Calculate the tick to start generating beatlines from
                currentTick < Waveform.endTick && // Don't generate beatlines outside of the shown time period
                currentTick < SongTimelineManager.SongLengthTicks; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            //Debug.Log($"{Time.frameCount} New beatline created. This tick: {currentTick}. Positioning: ({BPM.ConvertTickTimeToSeconds(currentTick)} - {Waveform.startTime}) => {(BPM.ConvertTickTimeToSeconds(currentTick) - Waveform.startTime)} / {Waveform.timeShown}, {boundaryReference.rect.height}");
            // Get a beatline to calculate data for
            var workedBeatline = BeatlinePooler.instance.GetBeatline(currentBeatline);
            workedBeatline.Tick = currentTick;

            workedBeatline.UpdateBeatlinePosition((Tempo.ConvertTickTimeToSeconds(currentTick) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);

            // Needed to generate correct thickness
            workedBeatline.Type = TimeSignature.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += TimeSignature.IncreaseByHalfDivision(currentTick);
        }

        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);
    }
}
