using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GuitarGem : BasicGem
{
    /// <summary>
    /// Is the gem a tap note?
    /// </summary>
    public bool IsTap { get; set; }

    /// <summary>
    /// Is the gem a forced note?
    /// </summary>
    public bool IsForced { get; set; }

    // THIS METHOD OF THIS FUNCTION DOES NOT WORK
    // DOES NOT ACCOUNT FOR CHORDS
    // CHORD PAIRS HAVE TWO NOTES, 1 FORCED FLAG, 1 TAP FLAG
    // commenting this out so i don't accidentally do this again down the line
    // this is on the right track though, with the additions & their checks
    // there are also other ways to simplify this function
    // public List<String> ExportGem()
    // {
    //     List<String> noteEvents = new()
    //     {
    //         $"{TickPosition} = N {Fret} {SustainLength}" // Add note itself
    //     }; // Create list to store note events to append to file

    //     if (IsTap) // Add tap flag if necessary
    //     {
    //         noteEvents.Add($"{TickPosition} = N 6 0");
    //     }
    //     else if (IsForced) // Add forced flag if necessary
    //     {
    //         noteEvents.Add($"{TickPosition} = N 5 0");
    //     }

    //     return noteEvents;
    // }
}
