using UnityEngine;
using System.Collections.Generic;
using System;

public class BeatlinePooler : MonoBehaviour
{
    // Adapted from Unity's intro to object pooling
    // https://learn.unity.com/tutorial/introduction-to-object-pooling

    [SerializeField] GameObject beatlinePrefab;
    [SerializeField] GameObject canvas;

    /// <summary>
    /// Static reference to the pooler object.
    /// </summary>
    public static BeatlinePooler instance;

    private List<Beatline> beatlines;
    
    public int poolAmount = 50;

    void Awake()
    {
        instance = this;
        beatlines = new();
    }

    void Start()
    {
        for(int i = 0; i < poolAmount; i++)
        {
            CreateNewBeatline();
        }
    }

    // All the beatlines are based on UI so they shouldn't (?) need to be scaled or anything weird like that
    void CreateNewBeatline()
    {
        GameObject tmp;
        tmp = Instantiate(beatlinePrefab, canvas.transform); // MUST BE A CHILD OF THE CANVAS
        beatlines.Add(tmp.GetComponent<Beatline>());
    
        beatlines[^1].IsVisible = false;
    }

    /// <summary>
    /// Get a specified beatline from the collection of existing pooled beatline objects.
    /// </summary>
    /// <param name="index">The target beatline number.</param>
    /// <returns>The requested beatline.</returns>
    public Beatline GetBeatline(int index)
    {
        Beatline beatline;
        try
        {
            beatline = beatlines[index]; // attempt fetch of the requested beatline
        }
        catch // if the requested beatline does not exist, make a new one and fetch it
        {
            CreateNewBeatline(); // this only needs to happen once because accessing a beatline will happen sequentially
            beatline = beatlines[index];
        }
        beatline.IsVisible = true; // prepare beatline to display calculations
        return beatline;
    }

    /// <summary>
    /// Deactivate all active beatlines past a specified beatline number.
    /// <para>Called after generating beatlines. Pass in the last generated beatline number to deactivate all unused beatlines past that beatline.</para>
    /// </summary>
    /// <param name="lastIndex">The beatline number to start deactivating from.</param>
    public void DeactivateUnusedBeatlines(int lastIndex)
    {
        // Since beatlines are accessed and displayed sequentially, disable all
        // beatlines from the last beatline accessed until hitting an already inactive beatline.
        while (true)
        {
            try 
            {
                if (beatlines[lastIndex].IsVisible) 
                {
                    beatlines[lastIndex].IsVisible = false;
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
