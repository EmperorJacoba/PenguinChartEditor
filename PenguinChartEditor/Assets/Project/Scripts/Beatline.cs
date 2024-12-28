using UnityEngine;

public class Beatline : MonoBehaviour
{
    /// <summary>
    /// The tempo value of the beatline, in integer form.
    /// <para>Example: 120.422 BPM = 120422</para>
    /// </summary>
    public int Tempo { get; set; }

    /// <summary>
    /// The location of the beatline, in tick-time.
    /// </summary>
    public int TickPosition { get; set; }

    /// <summary>
    /// Reformat beatline data for use in .chart file.
    /// </summary>
    /// <returns></returns>
    public string Export()
    {
        return $"{TickPosition} = {Tempo}";
    } // NOT FINAL, JUST CONCEPT -> MUST BE FORMATTED FOR FINAL DATA STRUCTURE (probably enum, but research others)
}
