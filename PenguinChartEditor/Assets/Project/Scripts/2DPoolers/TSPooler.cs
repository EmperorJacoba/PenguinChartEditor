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
}