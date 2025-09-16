using UnityEngine.UI;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    public static bool metronomeActive = false;
    const int TICK_BUFFER = 1;

    void Awake()
    {
        SongTimelineManager.TimeChanged += CheckForMetronomeHit; // more concise solution for checking for metronome over Update()
    }

    static int nextPromisedMetronomeHit = 0;

    // promised metronome hit will not be active on first loop
    // could result in first hit missing/skipped w/o this var
    bool firstLoop = true;
    void CheckForMetronomeHit()
    {
        // might change in case metronome for ffw/rw buttons is a wanted feature
        if (!AudioManager.AudioPlaying || !metronomeActive)
        {
            firstLoop = true;
            return;
        }

        if (firstLoop)
        {
            nextPromisedMetronomeHit = TimeSignature.GetNextDivisionEvent(SongTimelineManager.SongPositionTicks);
            firstLoop = false;
        }

        if (SongTimelineManager.SongPositionTicks >= nextPromisedMetronomeHit)
        {
            // BASS is more reliable, consistant, and all-around better
            // for any audio applications. Near-instant response from this
            // while audio component will delay by ~100ms or so
            AudioManager.PlayMetronomeSound();

            // Add a tick buffer (+1) so that the metronome will
            // not tick twice for the same tick 
            nextPromisedMetronomeHit = TimeSignature.GetNextDivisionEvent(SongTimelineManager.SongPositionTicks + TICK_BUFFER);
        }
    }

    public void ToggleMetronome()
    {
        metronomeActive = !metronomeActive;
    }
}