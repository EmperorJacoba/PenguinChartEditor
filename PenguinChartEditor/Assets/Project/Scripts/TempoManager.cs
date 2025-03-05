using System.Collections.Generic;
using UnityEngine;

public class TempoManager : MonoBehaviour
{    
    /// <summary>
    /// Dictionary that contains tempo changes and corresponding tick time positions. 
    /// <para> Key = Tick-time position. Value = BPM to three decimal places. </para>
    /// <para>Example: 192000 = 102.201</para>
    /// <para>When writing to file, multiply value by 100 to get proper .chart format (where example would show as B 192000 = 102201)</para>
    /// </summary>
    public Dictionary<int, float> TempoEvents {get; set;}

    /// <summary>
    /// Dictionary that contains timestamps to render beatlines at. Key = Beat number in quarter notes. Value = Timestamp.
    /// <para>Example: 28 = 10.225 -> Beat 28 should be rendered 10.225 seconds into the audio</para>
    /// <para>Example: 10.5 = 4.4112 -> The eighth note between beats 10 and 11 should be rendered 4.4112 seconds into the audio</para>
    /// </summary>
    public Dictionary<float, float> BeatlineTimestamps {get; set;} // I might not have to use this, but this is a port from my original sketch

    /// <summary>
    /// The thickness of a bar starting line
    /// </summary>
    private float barLineThickness = 0.05f;

    /// <summary>
    /// The thickness of the division line (e.g quarter note in 4/4 or eighth note in 7/8)
    /// </summary>
    private float divisionLineThickness = 0.03f;

    /// <summary>
    /// The thickness of the second division line (e.g eighth note in 4/4)
    /// </summary>
    private float halfDivisionLineThickness = 0.01f;
    

    void Awake()
    {
        TempoEvents = new();
        BeatlineTimestamps = new();
        WaveformManager.WFPositionChanged += WaveformMoved; // set up event so that beatlines can update properly
    }

    void Start()
    {
        if (TempoEvents.Count == 0) // if there is no data to load in 
        {
            TempoEvents.Add(0, 120000); // add placeholder bpm
            BeatlineTimestamps.Add(0, 0);
        }

    }

    /// <summary>
    /// This fires every time the visible waveform changes. Used to update beatlines to new displayed waveform.
    /// </summary>
    void WaveformMoved()
    {
        
    }

}
