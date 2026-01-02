using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NoteReceiver : MonoBehaviour
{
    Animator animator;
    [SerializeField] int lane;
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
        var lengthSeconds = Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(tick + sustainLength) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(SongTime.SongPositionTicks);
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
    bool nextIsOpen = false;
    void CheckForNoteHit()
    {
        if (firstLoop)
        {
            nextPromisedNoteHit = GetStartingNote();
            firstLoop = false;
        }

        if (SongTime.SongPositionTicks >= nextPromisedNoteHit)
        {
            var laneData = nextIsOpen ? Chart.GetActiveInstrument<FiveFretInstrument>().GetLaneData(FiveFretInstrument.LaneOrientation.open) :
                Chart.GetActiveInstrument<FiveFretInstrument>().GetLaneData(lane);

            var tickSustain = laneData[nextPromisedNoteHit].Sustain;
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
        nextIsOpen = false;

        var instrument = Chart.GetActiveInstrument<FiveFretInstrument>();
        var activeLaneTick = instrument.GetLaneData(lane).GetNextRelevantTick();
        var openLaneTick = instrument.GetLaneData(FiveFretInstrument.LaneOrientation.open).GetNextRelevantTick();

        var relevantTick = Mathf.Min(activeLaneTick, openLaneTick);

        if (relevantTick == openLaneTick) nextIsOpen = true;

        return relevantTick;
    }

    int GetStartingNote()
    {
        nextIsOpen = false;

        var instrument = Chart.GetActiveInstrument<FiveFretInstrument>();
        var activeLaneTick = instrument.GetLaneData(lane).GetFirstRelevantTick<FiveFretNoteData>();
        var openLaneTick = instrument.GetLaneData(FiveFretInstrument.LaneOrientation.open).GetFirstRelevantTick<FiveFretNoteData>();

        int relevantTick;
        if (activeLaneTick < SongTime.SongPositionTicks && openLaneTick < SongTime.SongPositionTicks)
        {
            relevantTick = Mathf.Max(activeLaneTick, openLaneTick);
        }
        relevantTick = Mathf.Min(activeLaneTick, openLaneTick);

        if (relevantTick == openLaneTick) nextIsOpen = true;

        return relevantTick;
    }
}
