using System.Collections;
using UnityEngine;

/// <summary>
/// Difficulty choices for a given instrument (e.g. E, M, H, X)
/// </summary>
public enum DifficultyType
{
    easy,
    medium,
    hard,
    expert
}

/// <summary>
/// The instrument that a given chunk of track data belongs to (guitar, bass, drums, etc.)
/// </summary>
public enum InstrumentType
{
    guitar,
    coopGuitar,
    rhythm,
    bass,
    keys,
    drums,
    ghlGuitar,
    ghlBass,
    ghlRhythm,
    vox
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
}