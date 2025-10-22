using System.Collections;
using UnityEngine;

/// <summary>
/// Difficulty choices for a given instrument (e.g. E, M, H, X)
/// </summary>
public enum DifficultyType
{
    easy = 0,
    medium = 1,
    hard = 2,
    expert = 3
}

/// <summary>
/// The instrument that a given chunk of track data belongs to (guitar, bass, drums, etc.)
/// <para>Parallel to HeaderType numbering.</para>
/// </summary>
public enum InstrumentType
{
    guitar = 10,
    coopGuitar = 20,
    rhythm = 30,
    bass = 40,
    keys = 50,
    drums = 100,
    ghlGuitar = 1000,
    ghlCoop = 1010,
    ghlBass = 1020,
    ghlRhythm = 1030,
    vox = 10000
}

/// <summary>
/// Stores valid types of audio stems.
/// </summary>
public enum StemType
{
    // 0 is reserved for none
    song = 1,
    guitar = 2,
    bass = 3,
    rhythm = 4,
    keys = 5,
    vocals = 6,
    vocals_1 = 7,
    vocals_2 = 8,
    drums = 9,
    drums_1 = 10,
    drums_2 = 11,
    drums_3 = 12,
    drums_4 = 13,
    crowd = 14
}