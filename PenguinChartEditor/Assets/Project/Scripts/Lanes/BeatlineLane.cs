using UnityEngine;
using UnityEngine.Timeline;

// This does not inherit from Lane<T> because it does not use event data
// Beatlines are specially generated based on time signature and tempo data
// Beatlines are not events themselves
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
        var currentTSEventTick = TimeSignature.GetLastTSEventTick(Waveform.startTick);
        for (
                int currentTick = TimeSignature.GetNextBeatlineEvent(Waveform.startTick); // Calculate the tick to start generating beatlines from
                currentTick < Waveform.endTick && // Don't generate beatlines outside of the shown time period
                currentTick < SongTime.SongLengthTicks; // Don't generate beatlines that don't exist (falls ahead of the end of the audio file) 
                currentBeatline++
            )
        {
            // If the user places a TS event on an irregular position (using 1/3 or 1/6 or 1/12 step)
            // the beatlines will generate based on the beggining TS event, but not based on the irregular TS event,
            // if it happens in the middle of a generation window. It skips over the TS event and generates nothing
            // after the badly placed TS event. This check prevents that from happening.
            if (TimeSignature.GetLastTSEventTick(currentTick) != currentTSEventTick)
            {
                currentTick = TimeSignature.GetLastTSEventTick(currentTick);
                currentTSEventTick = TimeSignature.GetLastTSEventTick(currentTick);
            }

            var workedBeatline = BeatlinePooler.instance.GetObject(currentBeatline);
            workedBeatline.InitializeEvent(currentTick, boundaryReference.rect.height);

            workedBeatline.UpdateBeatlinePosition(Waveform.GetWaveformRatio(currentTick), boundaryReference.rect.height);

            // Needed to generate correct thickness
            workedBeatline.Type = TimeSignature.CalculateBeatlineType(currentTick);

            // Set up tick for next beatline's calculations
            currentTick += TimeSignature.IncreaseByHalfDivision(currentTick);
        }

        BeatlinePooler.instance.DeactivateUnused(currentBeatline);
    }
}
