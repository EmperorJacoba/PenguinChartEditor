using System;
using UnityEngine;

public class SelfDestroyingDebugObject : MonoBehaviour
{
    private void Awake()
    {
        // This is so that scenes can be run standalone (no tabs) while also suppressing unity errors due to multiple
        // cameras with audio listeners and multiple event systems.
        if (TabSceneSpawningManager.IsTabbingActive())
        {
            Destroy(gameObject);
        }
    }
}