using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BeatlinePooler3D : Pooler<Beatline3D>
{
    /// <summary>
    /// Static reference to the pooler object.
    /// </summary>
    public static BeatlinePooler3D instance;

    void Awake()
    {
        instance = this;
    }
}
