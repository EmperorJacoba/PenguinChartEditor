using Un4seen.Bass;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    [SerializeField] AudioSource metronomeClick;
    void Awake()
    {
        SongTimelineManager.TimeChanged += CheckForMetronomeHit;
    }

    static int nextPromisedMetronomeHit = 0;
    bool firstLoop = true;
    void CheckForMetronomeHit()
    {
        if (!PluginBassManager.AudioPlaying)
        {
            firstLoop = true;
            return;
        }

        if (firstLoop)
        {
            nextPromisedMetronomeHit = TimeSignature.FindNextDivisionEvent(SongTimelineManager.SongPositionTicks);
            firstLoop = false;
        }

        if (SongTimelineManager.SongPositionTicks >= nextPromisedMetronomeHit)
        {
            PluginBassManager.PlayMetronomeSound();
            nextPromisedMetronomeHit = TimeSignature.FindNextDivisionEvent(SongTimelineManager.SongPositionTicks + 1);
        }
    }
}