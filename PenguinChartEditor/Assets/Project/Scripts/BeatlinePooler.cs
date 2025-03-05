using UnityEngine;
using System.Collections.Generic;

public class BeatlinePooler : MonoBehaviour
{
    // Most of this code is copied straight from Unity's intro to object pooling
    // Work smarter, not harder, kids! (Thanks, Unity staff!)
    // https://learn.unity.com/tutorial/introduction-to-object-pooling

    [SerializeField] GameObject beatlinePrefab;

    public static BeatlinePooler instance;
    public List<GameObject> pooledObjects;
    public int poolAmount;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        pooledObjects = new List<GameObject>();
        for(int i = 0; i < poolAmount; i++)
        {
            CreateNewObject();
        }
    }

    void CreateNewObject()
    {
        GameObject tmp;
        tmp = Instantiate(beatlinePrefab, transform);
        tmp.SetActive(false);
        pooledObjects.Add(tmp);
    }

    public GameObject GetPooledObject()
    {
        for(int i = 0; i < poolAmount; i++)
        {
            if(!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }
        // This prevents a null return by creating a new game object and recalling the function if no more are available.
        CreateNewObject();
        return GetPooledObject();
    }

    // When instantiating initial objects:
    // Instantiate with width of tempo background track by screen percentage (ratio)
        // Recalculate ratio when screen is changed (probably an event you can subscribe to here)
    // Line thicknesses are determined by TempoManager

    // Next goal: Set up this script to properly set up prefabs with ratio
    // Then: Spawn in a beatline with TempoManager
    // Then: Spawn in a series of beatlines in accordance with time-second markings
        // Experiment with spawning beatlines even when there are no timestamps -> inbetween, dynamically determined beatlines
            // Every time-second calculation in the dict will correspond to a TempoEvent
            // Maybe combine dictionaries?
        // Experiment with moving beatlines
            // They move only in Y-direction -> X-dir is locked
    // Then: Use an existing SyncTrack to generate beatlines
        // Read a .chart file
        // Get [SyncTrack] section
        // Parse section data into TempoEvents
        // Calculate beatline positions from new data
        // Render beatlines
}
