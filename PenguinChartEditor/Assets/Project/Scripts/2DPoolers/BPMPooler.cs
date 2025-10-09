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

    public override void DeactivateUnused(int lastIndex)
    {
        // Since beatlines are accessed and displayed sequentially, disable all
        // beatlines from the last beatline accessed until hitting an already inactive beatline.
        while (true)
        {
            try
            {
                if (eventObjects[lastIndex].Visible)
                {
                    eventObjects[lastIndex].Visible = false;
                }
                else break;
            }
            catch
            {
                break;
            }
            lastIndex++;
        }
    }
}