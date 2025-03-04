using UnityEngine;

public class TimeSignature : MonoBehaviour
{
    /// <summary>
    /// Number of beats per bar.
    /// <para>Example: 3/4 timesignature -> beatsPerBar = 3</para>
    /// </summary>
    public int BeatsPerBar { get; set; }

    /// <summary>
    /// Type of note to subdivide into.
    /// <para>Example: 3/4 timesignature -> beatDivision = 4 (quarter note)</para>
    /// </summary>
    public int BeatDivision { get; set; }

    /// <summary>
    /// The location of the time signature change, in tick-time.
    /// </summary>
    public int TickPosition { get; set; }
    
    /// <summary>
    /// Reformats the time signature so that it can be inserted into .chart file.
    /// </summary>
    /// <returns>"TS {num} {denomInLogBase2}"</returns>
    public string Export()
    {
        float denomInLogBase2 = Mathf.Log(BeatDivision) / Mathf.Log(2);

        return $"TS {BeatsPerBar} {denomInLogBase2}";
    } // NOT FINAL, JUST CONCEPT -> MUST BE FORMATTED FOR FINAL DATA STRUCTURE (probably enum, but research others)
}