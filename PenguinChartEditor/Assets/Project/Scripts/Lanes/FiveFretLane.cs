using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FiveFretLane : SpawningLane<FiveFretNote>
{
    [HideInInspector] public FiveFretInstrument.LaneOrientation laneIdentifier;

    [SerializeField] FiveFretNotePooler lanePooler;
    FiveFretNotePreviewer previewer;

    // notes rely on this for their lane's sustain data
    public SustainData<FiveFretNoteData> sustainData = new();

    protected override IPooler<FiveFretNote> Pooler => (IPooler<FiveFretNote>)lanePooler;
    protected override IPreviewer Previewer => previewer;

    protected override void Awake()
    {
        base.Awake();
        previewer = transform.GetChild(0).gameObject.GetComponent<FiveFretNotePreviewer>();
        Chart.ChartTabUpdated += UpdateEvents;
        AudioManager.PlaybackStateChanged += playing => { if (!playing) UpdateEvents(); };
    }

    protected override List<int> GetEventsToDisplay()
    {
        var workingLane = (LaneSet<FiveFretNoteData>)parentGameInstrument.representedInstrument.GetLaneData((int)laneIdentifier);

        // creating the events list is incredibly slow
        // perhaps get the keys and then binary search?
        if (AudioManager.AudioPlaying)
        {
            var events = GetPlaybackTickRange(workingLane, SongTime.SongPositionTicks);

            if (events.Count > 0)
            {
                var sustainCandidate = events[0];
                if (
                    sustainCandidate + workingLane[sustainCandidate].Sustain > SongTime.SongPositionTicks && 
                    sustainCandidate < SongTime.SongPositionTicks
                    )
                {
                    sustainOnlyTick = sustainCandidate;
                }
                else
                {
                    sustainOnlyTick = -1;
                }
            }

            return events;
        }
        else
        {
            sustainOnlyTick = -1;
            return GetPlaybackTickRange(workingLane, Waveform.startTick);
        }
    }

    private static List<int> GetPlaybackTickRange(LaneSet<FiveFretNoteData> workingLane, int lowerBound)
    {
        return workingLane.Where(
        tick => tick.Key < Waveform.endTick && tick.Key + tick.Value.Sustain > lowerBound
        ).Select(item => item.Key).ToList();
    }

    int sustainOnlyTick = -1;
    protected override void InitializeEvent(FiveFretNote @event, int tick)
    {
        @event.InitializeEvent(this, tick, tick == sustainOnlyTick);
    }
}