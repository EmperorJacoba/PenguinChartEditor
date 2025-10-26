using System;
using System.Collections.Generic;
using UnityEngine;

// Each lane has its own set of notes and selections (EventData)
// Lanes are defined with type T.
// Notes are defined/calculated upon on a per-lane basis.

public class FiveFretNote : Event<FiveFretNoteData>, IPoolable
{
    static MoveData<FiveFretNoteData> moveData = new(); // needs data broker
    public override MoveData<FiveFretNoteData> GetMoveData() => moveData;

    static EventData<FiveFretNoteData> eventData = new(); // needs data broker
    public override EventData<FiveFretNoteData> GetEventData() => eventData;

    public Coroutine destructionCoroutine { get; set; }

    public override IPreviewer EventPreviewer => lanePreviewer;
    public IPreviewer lanePreviewer; // define in pooler

    FiveFretLane parentLane
    {
        get
        {
            if (_lane == null)
            {
                _lane = GetComponentInParent<FiveFretLane>();
            }
            return _lane;
        }
    } // define in pooler
    FiveFretLane _lane;

    public override void RefreshEvents()
    {
        parentLane.UpdateEvents();
    }

    public override SortedDictionary<int, FiveFretNoteData> GetEventSet()
    {
        var instrument = (FiveFretInstrument)Chart.LoadedInstrument;
        return instrument.Lanes[(int)laneIdentifier];
    }

    public override void SetEvents(SortedDictionary<int, FiveFretNoteData> newEvents)
    {
        var instrument = (FiveFretInstrument)Chart.LoadedInstrument;
        instrument.Lanes[(int)laneIdentifier] = newEvents;
    }

    public FiveFretInstrument.LaneOrientation laneIdentifier;

    public void InitializeNote()
    {
        Selected = CheckForSelection();
    }

    public void UpdatePosition(double percentOfTrack, float trackLength, float xPosition)
    {
        var trackProportion = (float)percentOfTrack * trackLength;
        transform.position = new Vector3(xPosition, 0, trackProportion);
    }
} 