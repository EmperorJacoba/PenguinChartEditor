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

            workedBeatline.BPMLabelText = $"{currentTick}";

            // Timestamp is calculated before loop starts, so start by updating the selected beatline's position
            workedBeatline.UpdateBeatlinePosition((SongTimelineManager.ConvertTickTimeToSeconds(currentTick) - startTime)/timeShown); 

            workedBeatline.Type = SongTimelineManager.CalculateBeatlineType(currentTick);
            workedBeatline.IsVisible = true;

            currentTick += SongTimelineManager.PLACEHOLDER_RESOLUTION / SongTimelineManager.CalculateDivision(currentTick) / 2;
        }
        BeatlinePooler.instance.DeactivateUnusedBeatlines(currentBeatline);

        // Sweep for special labels (irregular beatline label placement) here

        // take the first beatline in the valid timestamps -> generate that beatline, beatline & label visible
        // get distance between beatlines based on paired BPM, make next beatlines based on that until you hit next timestamp -> beatline visible but label invisible
            // at that timestamp, recalculate distance to the next beatline
            // this can also happen in the middle of two beatlines -> if this happens, keep the label, hide the beatline
                // find distance between current division and main division, and then render bpm normally
                // example: 0 -> 192 -> 384 -> 576 -> 768 is standard quarter note tick-timestamps
                // if one falls on tick 64, you need to render where the 192 beatline would be if BPM was on tick 0 
                // this logic can fit in for all beatlines
    }



    
    // 192 / 4 = 48 = sixteenth note
    // 192 / 2 = 96 = eighth note
    // 192 / 1 = 192 = quarter note
    // 192 / 0.5 = 384 = half note
    // 192 / 0.25 = 768 = whole note

    // Next steps:
    // Make finding valid tempo events more efficient
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
