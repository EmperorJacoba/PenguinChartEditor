using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NoteReceiver : MonoBehaviour
{
    Animator animator;
    [SerializeField] FiveFretInstrument.LaneOrientation lane;
    bool firstLoop = true;

    void Play()
    {
        animator.Play("Punch");
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
            SongTime.TimeChanged -= CheckForNoteHit;
        }
    }

    int nextPromisedNoteHit = -1;
    void CheckForNoteHit()
    {
        if (firstLoop)
        {
            nextPromisedNoteHit = GetNextNoteHit();
            firstLoop = false;
        }

        if (SongTime.SongPositionTicks >= nextPromisedNoteHit)
        {
            Play();
            nextPromisedNoteHit = GetNextNoteHit();
        }
    }

    int GetNextNoteHit()
    {
        return Chart.GetActiveInstrument<FiveFretInstrument>().
            Lanes.GetLane((int)lane).
            GetNextTickEventInLane(SongTime.SongPositionTicks);
    }
}
