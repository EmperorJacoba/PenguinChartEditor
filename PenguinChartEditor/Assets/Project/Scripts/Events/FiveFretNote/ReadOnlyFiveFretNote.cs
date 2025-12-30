using UnityEngine;

public class ReadOnlyFiveFretNote : ReadOnlyEvent<FiveFretNoteData>
{
    public override LaneSet<FiveFretNoteData> LaneData => throw new System.NotImplementedException();
}