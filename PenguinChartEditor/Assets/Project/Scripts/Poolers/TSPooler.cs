using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TSPooler : Pooler<TSLabel>
{
    public static TSPooler instance;

    void Awake()
    {
        instance = this;
    }
    public override TSLabel ActivateObject(int index, int activationTick, float highwayLength)
    {
        var label = base.ActivateObject(index, activationTick, highwayLength);
        label.InitializeEvent(activationTick, highwayLength);
        return label;
    }
}