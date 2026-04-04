using System;
using System.Collections.Generic;
using UnityEngine;

public class TabLoader : MonoBehaviour
{
    [SerializeField] private List<GameObject> groups;

    // some things ended up returning null if not initialized. It looks ugly in the editor if I make them all visible
    // before playing to avoid this. So just quickly turn them on and off again to avoid this.
    private void Awake()
    {
        foreach (var obj in groups)
        {
            var originalState = obj.activeInHierarchy;
            obj.SetActive(!originalState);
            obj.SetActive(originalState);
        }
        Destroy(gameObject);
    }
}