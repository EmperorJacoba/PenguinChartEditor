using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NoteReceiver : MonoBehaviour
{
    Animator animator;
    [SerializeField] FiveFretInstrument.LaneOrientation lane;
    bool firstLoop = true;

    void PlayNoSustain()
    {
        animator.Play("Punch", 0, 0);
    }

    void PlaySustain(int tick, int sustainLength)
    {
        animator.Play("SustainPunch", 0, 0);
        StartCoroutine(StopSustainAfterLength(tick, sustainLength));
    }

    void PlayIdle()
    {
        animator.Play("Idle", 0, 0);
    }

    void PlayFall()
    {
        animator.Play("SustainFall", 0, 0);
    }

    IEnumerator StopSustainAfterLength(int tick, int sustainLength)
    {
        var lengthSeconds = Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick + sustainLength) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick);
        yield return new WaitForSeconds((float)lengthSeconds);
        if (AudioManager.AudioPlaying) PlayFall();
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        AudioManager.PlaybackStateChanged += x => ToggleNoteReceivers(x);
    }

    void ToggleNoteReceivers(bool state)
    {
        if (state)
        {
            firstLoop = true;
            SongTime.TimeChanged += CheckForNoteHit;
        }
        else
        {
            PlayIdle();
            SongTime.TimeChanged -= CheckForNoteHit;
        }
    }

    int nextPromisedNoteHit = -1;
    void CheckForNoteHit()
    {
        if (firstLoop)
        {
            nextPromisedNoteHit = GetStartingNote();
            firstLoop = false;
        }

        if (SongTime.SongPositionTicks >= nextPromisedNoteHit)
        {
            var tickSustain = Chart.GetActiveInstrument<FiveFretInstrument>().Lanes.GetLane((int)lane)[nextPromisedNoteHit].Sustain;
            if (tickSustain > 0)
            {
                PlaySustain(nextPromisedNoteHit, tickSustain);
            }
            else
            {
                PlayNoSustain();
            }
            nextPromisedNoteHit = GetNextNoteHit();
        }
    }

    int GetNextNoteHit()
    {

        return Chart.GetActiveInstrument<FiveFretInstrument>().
            Lanes.GetLane((int)lane).
            GetNextTickEventInLane(SongTime.SongPositionTicks);
    }

    int GetStartingNote()
    {
        var active = Chart.GetActiveInstrument<FiveFretInstrument>().Lanes.GetLane((int)lane);
        var prevEvent = active.GetPreviousTickEventInLane(SongTime.SongPositionTicks, inclusive: true);
        var nextEvent = active.GetNextTickEventInLane(SongTime.SongPositionTicks);

        if (prevEvent == LaneSet<FiveFretNoteData>.NO_TICK_EVENT)
            return ValidateEvent(nextEvent);

        if (prevEvent + active[prevEvent].Sustain > SongTime.SongPositionTicks) return prevEvent;
        
        return ValidateEvent(nextEvent);
    }

    int ValidateEvent(int nextEvent) => nextEvent == LaneSet<FiveFretNoteData>.NO_TICK_EVENT ? SongTime.SongLengthTicks : nextEvent;
}
