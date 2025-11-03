
public class FiveFretNotePooler : Pooler<FiveFretNote>
{
    public FiveFretInstrument.LaneOrientation lane;
    public override FiveFretNote ActivateObject(int index, int activationTick, float highwayLength)
    {
        var fiveFretNote = base.ActivateObject(index, activationTick, highwayLength);
        fiveFretNote.InitializeEvent(activationTick, highwayLength, lane);
        fiveFretNote.lanePreviewer = parentObject.GetComponent<FiveFretLane>().previewer;
        return fiveFretNote;
    }
}
