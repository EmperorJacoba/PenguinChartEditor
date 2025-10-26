using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FiveFretNotePooler : Pooler<FiveFretNote>
{
    public override FiveFretNote ActivateObject(int index, int activationTick)
    {
        var fiveFretNote = base.ActivateObject(index, activationTick);
        fiveFretNote.lanePreviewer = parentObject.GetComponent<FiveFretLane>().previewer;
        return fiveFretNote;
    }
}
