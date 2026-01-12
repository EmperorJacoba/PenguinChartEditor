using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NoteReceiver : MonoBehaviour
{
    #region Animation Management

    Animator animator;

    // 0, 0 arguments make it possible for restarting animations
    // without cooldowns for sections with lots of notes back-to-back.
    // Please do not mess with these unless you absolutely have to.
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

    #endregion

    [SerializeField] int lane;
    Strikeline3D strikeline;
    IInstrument ParentInstrument => strikeline.parentGameInstrument.representedInstrument;
    bool firstLoop = true;
    IEnumerator StopSustainAfterLength(int tick, int sustainLength)
    {
        var lengthSeconds = Chart.SyncTrackInstrument.ConvertTickDurationToSeconds(SongTime.SongPositionTicks, tick + sustainLength) * AudioManager.currentAudioSpeed;
        yield return new WaitForSeconds((float)lengthSeconds);
        if (AudioManager.AudioPlaying) PlayFall();
    }

    private void Awake()
    {
        strikeline = GetComponentInParent<Strikeline3D>();
        animator = GetComponent<Animator>();
    }

    AudioManager.PlayingDelegate activeDelegateAction;
    private void OnEnable()
    {
        activeDelegateAction = x => ToggleNoteReceivers(x);
        AudioManager.PlaybackStateChanged += activeDelegateAction;
    }

    private void OnDisable()
    {
        activeDelegateAction -= activeDelegateAction;
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
    bool nextIsBar = false;
    void CheckForNoteHit()
    {
        if (firstLoop)
        {
            nextPromisedNoteHit = GetStartingNote();
            firstLoop = false;
        }

        if (SongTime.SongPositionTicks >= nextPromisedNoteHit)
        {
            var laneData = nextIsBar ? ParentInstrument.GetBarLaneData() :
                ParentInstrument.GetLaneData(lane);

            var tickSustain = laneData.GetTickSustain(nextPromisedNoteHit);
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
        nextIsBar = false;

        var activeLaneTick = ParentInstrument.GetLaneData(lane).GetNextRelevantTick();
        var openLaneTick = ParentInstrument.GetBarLaneData().GetNextRelevantTick();

        var relevantTick = Mathf.Min(activeLaneTick, openLaneTick);

        if (relevantTick == openLaneTick) nextIsBar = true;

        return relevantTick;
    }

    int GetStartingNote()
    {
        nextIsBar = false;

        var activeLaneTick = ParentInstrument.GetLaneData(lane).GetFirstRelevantTick();
        var openLaneTick = ParentInstrument.GetBarLaneData().GetFirstRelevantTick();

        int relevantTick;
        if (activeLaneTick < SongTime.SongPositionTicks && openLaneTick < SongTime.SongPositionTicks)
        {
            relevantTick = Mathf.Max(activeLaneTick, openLaneTick);
        }
        relevantTick = Mathf.Min(activeLaneTick, openLaneTick);

        if (relevantTick == openLaneTick) nextIsBar = true;

        return relevantTick;
    }
}
