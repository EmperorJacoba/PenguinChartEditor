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

}