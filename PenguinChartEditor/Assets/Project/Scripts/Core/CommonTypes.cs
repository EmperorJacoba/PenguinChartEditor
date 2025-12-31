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

public enum InstrumentCategory
{
    None = 1,
    FiveFret = 10,
    FourLaneDrums = 100,
    GHL = 1000,
    Vox = 10000
}

/// <summary>
/// The instrument that a given chunk of track data belongs to (guitar, bass, drums, etc.)
/// <para>Parallel to HeaderType numbering.</para>
/// </summary>
public enum InstrumentType
{
    synctrack = 1,
    starpower = 3,
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

public enum LocalEventIdentifier
{
    solo,
    soloend
}

/// <summary>
/// Contains possible section headers enclosed as [Name] in a .chart/.penguin file.
/// Identifiers follow a pattern based on instrument parsing needs. Metadata/tempo has values 10^0, Five-fret is 10^1, Drums is 10^2, GHL is 10^3, Vox is 10^4.
/// Difficulties: E = 0, M = 1, H = 2, X = 3
/// <para> Example: Song = 0, EasySingle (Easy Guitar) = 10, MediumDrums = 101 </para>
/// </summary>
public enum HeaderType
{
    Song = 0,
    SyncTrack = 1,
    Events = 2,
    Starpower = 3,

    EasySingle = 10,
    MediumSingle = 11,
    HardSingle = 12,
    ExpertSingle = 13,

    EasyDoubleGuitar = 20,
    MediumDoubleGuitar = 21,
    HardDoubleGuitar = 22,
    ExpertDoubleGuitar = 23,

    EasyDoubleBass = 30,
    MediumDoubleBass = 31,
    HardDoubleBass = 32,
    ExpertDoubleBass = 33,

    EasyDoubleRhythm = 40,
    MediumDoubleRhythm = 41,
    HardDoubleRhythm = 42,
    ExpertDoubleRhythm = 43,

    EasyKeyboard = 50,
    MediumKeyboard = 51,
    HardKeyboard = 52,
    ExpertKeyboard = 53,

    EasyDrums = 100,
    MediumDrums = 101,
    HardDrums = 102,
    ExpertDrums = 103,

    EasyGHLGuitar = 1000,
    MediumGHLGuitar = 1001,
    HardGHLGuitar = 1002,
    ExpertGHLGuitar = 1003,

    EasyGHLBass = 1010,
    MediumGHLBass = 1011,
    HardGHLBass = 1012,
    ExpertGHLBass = 1013,

    EasyGHLCoop = 1020,
    MediumGHLCoop = 1021,
    HardGHLCoop = 1022,
    ExpertGHLCoop = 1023,

    EasyGHLRhythm = 1030,
    MediumGHLRhythm = 1031,
    HardGHLRhythm = 1032,
    ExpertGHLRhythm = 1033,

    Vox = 10000, // no difficulties
}

public enum SceneType
{
    setup,
    tempoMap,
    fiveFretChart,
    starpower,
}