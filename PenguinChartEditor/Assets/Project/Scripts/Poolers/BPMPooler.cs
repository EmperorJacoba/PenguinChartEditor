using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMPooler : Pooler<BPMLabel>
{
    public static BPMPooler instance;

    void Awake()
    {
        instance = this;
    }
    public override BPMLabel ActivateObject(int index, int activationTick, float highwayLength)
    {
        var label = base.ActivateObject(index, activationTick, highwayLength);
        label.InitializeEvent(activationTick, highwayLength);
        return label;
    }

}