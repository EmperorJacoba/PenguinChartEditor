using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarningPooler : Pooler<Warning>
{
    public static WarningPooler instance;
    void Awake()
    {
        instance = this;
    }
}