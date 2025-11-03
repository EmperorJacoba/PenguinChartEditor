using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BeatlinePooler : Pooler<Beatline>
{
    /// <summary>
    /// Static reference to the pooler object.
    /// </summary>
    public static BeatlinePooler instance;

    void Awake()
    {
        instance = this;
    }

    public override Beatline ActivateObject(int index, int activationTick, float highwayLength)
    {
        var beat = base.ActivateObject(index, activationTick, highwayLength);
        beat.InitializeEvent(activationTick, highwayLength);
        return beat;
    }
}
