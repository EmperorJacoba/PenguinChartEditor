using UnityEngine;

public abstract class BasicGem : MonoBehaviour
{
    /// <summary>
    /// The fret number of the gem object.
    /// Number depends on instrument.
    /// </summary>
    public int Fret { get; set;}

    /// <summary>
    /// The sustain length of the gem object in ticks.
    /// </summary>
    public int SustainLength { get; set; }

    /// <summary>
    /// The position of the note on the track in tick-time.
    /// </summary>
    public int TickPosition { get; set; }

    /// <summary>
    /// Set up basic qualities of a note upon placement
    /// </summary>
    /// <param name="fret">The fret of the note (0-4, 7)</param>
    /// <param name="tick">The tick position of the note</param>
    public void InitGem(int fret, int tick)
    {
        Fret = fret;
        TickPosition = tick;
    }
}
