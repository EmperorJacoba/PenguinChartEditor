using System;
using System.Collections.Generic;
using UnityEngine;

// Each lane has its own set of notes and selections (EventData)
// Lanes are defined with type T.
// Notes are defined/calculated upon on a per-lane basis.

/*
public class FiveFretNote<T> : Event<FiveFretNoteData> where T : IFiveFretLane
{
    public static EventData<FiveFretNoteData> EventData = new();
    public override EventData<FiveFretNoteData> GetEventData() => EventData;
    public override SortedDictionary<int, FiveFretNoteData> GetEventSet()
    {
        throw new NotImplementedException();
    }

    static MoveData<FiveFretNoteData> moveData = new();
    public override MoveData<FiveFretNoteData> GetMoveData() => moveData;
    public override void SetEvents(SortedDictionary<int, FiveFretNoteData> newEvents)
    {
        throw new NotImplementedException();
    }
} */