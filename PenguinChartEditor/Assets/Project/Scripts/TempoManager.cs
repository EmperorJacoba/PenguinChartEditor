using UnityEngine;

public class TempoManager : MonoBehaviour
{  
    private WaveformManager waveformManager;
    private Strikeline strikeline;
    
    void Awake()
    {
        waveformManager = GameObject.Find("WaveformManager").GetComponent<WaveformManager>();
        strikeline = GameObject.Find("Strikeline").GetComponent<Strikeline>();

        SongTimelineManager.TimeChanged += SongTimeChanged; // set up event so that beatlines can update properly
        WaveformManager.DisplayChanged += SongTimeChanged;
    }

    void Start()
    {
    }

    /// <summary>
    /// Fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    void SongTimeChanged()
    {
        // Get the period of time shown on screen and the amount of time shown for position and bounds calculations 
        (var startTime, var endTime) = waveformManager.GetDisplayedAudio();
        int startTick = SongTimelineManager.ConvertSecondsToTickTime((float)startTime);
        int endTick = SongTimelineManager.ConvertSecondsToTickTime((float)endTime);
        var timeShown = endTime - startTime;

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

            if (SongTimelineManager.TempoEvents.ContainsKey(currentTick))
            {
                {Debug.Log($"{SongTimelineManager.TempoEvents[currentTick]}");}
                workedBeatline.BPMLabelVisible = true;
                workedBeatline.BPMLabelText = SongTimelineManager.TempoEvents[currentTick].Item1.ToString();
            }
            else
            {
                workedBeatline.BPMLabelVisible = false;
            }

            // Timestamp is calculated before loop starts, so start by updating the selected beatline's position
            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);
            workedBeatline.IsVisible = true;

            currentTick += SongTimelineManager.PLACEHOLDER_RESOLUTION / SongTimelineManager.CalculateDivision(currentTick) / 2;
        }
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
