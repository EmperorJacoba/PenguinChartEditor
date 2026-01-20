using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

public enum InstrumentCategory
{
    None = 1,
    FiveFret = 10,
    FourLaneDrums = 100,
    EliteDrums = 110,
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
    bass = 30,
    rhythm = 40,
    keys = 50,
    drums = 100,
    eliteDrums = 110,
    ghlGuitar = 1000,
    ghlBass = 1010,
    ghlCoop = 1020,
    ghlRhythm = 1030,
    vox = 10000,
    harmonies = 10010
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
/// Identifiers follow a pattern based on instrument parsing needs. Metadata/tempo/other specially parsed data has values 10^0, Five-fret is 10^1, Drums is 10^2, GHL is 10^3, Vox is 10^4.
/// Difficulties: E = 0, M = 1, H = 2, X = 3
/// <para> Example: Song = 0, EasySingle (Easy Guitar) = 10, MediumDrums = 101 </para>
/// </summary>
public enum HeaderType
{
    [InstrumentInformation("Metadata")]
    Song = 0,

    [InstrumentInformation("Tempo Map")]
    SyncTrack = 1,

    [InstrumentInformation("Events")]
    Events = 2,

    [InstrumentInformation("Starpower")]
    Starpower = 3,

    // ---- //

    [InstrumentInformation("Easy Guitar")]
    EasySingle = 10,

    [InstrumentInformation("Medium Guitar")]
    MediumSingle = 11,

    [InstrumentInformation("Hard Guitar")]
    HardSingle = 12,

    [InstrumentInformation("Expert Guitar")]
    ExpertSingle = 13,

    // ---- //

    [InstrumentInformation("Easy Coop Guitar")]
    EasyDoubleGuitar = 20,

    [InstrumentInformation("Medium Coop Guitar")]
    MediumDoubleGuitar = 21,

    [InstrumentInformation("Hard Coop Guitar")]
    HardDoubleGuitar = 22,

    [InstrumentInformation("Expert Coop Guitar")]
    ExpertDoubleGuitar = 23,

    // ---- //

    [InstrumentInformation("Easy Bass")]
    EasyDoubleBass = 30,

    [InstrumentInformation("Medium Bass")]
    MediumDoubleBass = 31,

    [InstrumentInformation("Hard Bass")]
    HardDoubleBass = 32,

    [InstrumentInformation("Expert Bass")]
    ExpertDoubleBass = 33,

    // ---- //

    [InstrumentInformation("Easy Rhythm")]
    EasyDoubleRhythm = 40,

    [InstrumentInformation("Medium Rhythm")]
    MediumDoubleRhythm = 41,

    [InstrumentInformation("Hard Rhythm")]
    HardDoubleRhythm = 42,

    [InstrumentInformation("Expert Rhythm")]
    ExpertDoubleRhythm = 43,

    // ---- //

    [InstrumentInformation("Easy Keys")]
    EasyKeyboard = 50,

    [InstrumentInformation("Medium Keys")]
    MediumKeyboard = 51,

    [InstrumentInformation("Hard Keys")]
    HardKeyboard = 52,

    [InstrumentInformation("Expert Keys")]
    ExpertKeyboard = 53,

    // ---- //

    [InstrumentInformation("Easy Drums")]
    EasyDrums = 100,

    [InstrumentInformation("Medium Drums")]
    MediumDrums = 101,

    [InstrumentInformation("Hard Drums")]
    HardDrums = 102,

    [InstrumentInformation("Expert Drums")]
    ExpertDrums = 103,

    // ---- //

    [InstrumentInformation("Easy Elite Drums")]
    EasyEliteDrums = 110,

    [InstrumentInformation("Medium Elite Drums")]
    MediumEliteDrums = 111,

    [InstrumentInformation("Hard Elite Drums")]
    HardEliteDrums = 112,

    [InstrumentInformation("Expert Elite Drums")]
    ExpertEliteDrums = 113,

    // ---- //

    [InstrumentInformation("Easy GHL Guitar")]
    EasyGHLGuitar = 1000,

    [InstrumentInformation("Medium GHL Guitar")]
    MediumGHLGuitar = 1001,

    [InstrumentInformation("Hard GHL Guitar")]
    HardGHLGuitar = 1002,

    [InstrumentInformation("Expert GHL Guitar")]
    ExpertGHLGuitar = 1003,

    // ---- //

    [InstrumentInformation("Easy GHL Bass")]
    EasyGHLBass = 1010,

    [InstrumentInformation("Medium GHL Bass")]
    MediumGHLBass = 1011,

    [InstrumentInformation("Hard GHL Bass")]
    HardGHLBass = 1012,

    [InstrumentInformation("Expert GHL Bass")]
    ExpertGHLBass = 1013,

    // ---- //

    [InstrumentInformation("Easy GHL Coop Guitar")]
    EasyGHLCoop = 1020,

    [InstrumentInformation("Medium GHL Coop Guitar")]
    MediumGHLCoop = 1021,

    [InstrumentInformation("Hard GHL Coop Guitar")]
    HardGHLCoop = 1022,

    [InstrumentInformation("Expert GHL Coop Guitar")]
    ExpertGHLCoop = 1023,

    // ---- //

    [InstrumentInformation("Easy GHL Rhythm")]
    EasyGHLRhythm = 1030,

    [InstrumentInformation("Medium GHL Rhythm")]
    MediumGHLRhythm = 1031,

    [InstrumentInformation("Hard GHL Rhythm")]
    HardGHLRhythm = 1032,

    [InstrumentInformation("Expert GHL Rhythm")]
    ExpertGHLRhythm = 1033,

    // ---- //

    [InstrumentInformation("Vocals")]
    Vox = 10000, // no difficulties

    [InstrumentInformation("Harmonies")]
    Harmony = 10010
}

public class InstrumentInformationAttribute : Attribute
{
    public string Name;
    public InstrumentInformationAttribute(string readableName)
    {
        Name = readableName;
    }
}

public static class InstrumentMetadata
{
    private static InstrumentInformationAttribute GetAttributeOnInstrumentID(HeaderType instrumentID) => (InstrumentInformationAttribute)instrumentID.GetType().GetCustomAttributes(typeof(InstrumentInformationAttribute), true).First();

    public static string GetInstrumentName(HeaderType instrumentID)
    {
        return GetAttributeOnInstrumentID(instrumentID).Name;
    }

    public static HeaderType GetHeader(InstrumentType instrument, DifficultyType difficulty)
    {
        return (HeaderType)((int)instrument + (int)difficulty);
    }

    public static string MakeChartLine(int tickKey, string value)
    {
        return $"\t{tickKey} = {value}";
    }

    public static string CreateHeader(HeaderType headerType)
    {
        return $"[{headerType}]\n{{";
    }

    public static void CreateHeader(StringBuilder stringifier, HeaderType headerType)
    {
        stringifier.AppendLine($"[{headerType}]");
        stringifier.AppendLine("{");
    }

    public static void CloseSection(StringBuilder stringifier)
    {
        stringifier.AppendLine("}");
    }

    public static bool TryParseHeaderType(string lineWithID, out HeaderType headerType)
    {
        var cleanIdentifier = lineWithID.Replace("[", "").Replace("]", "");
        if (!Enum.TryParse(typeof(HeaderType), cleanIdentifier, true, out var enumObject))
        {
            Debug.LogWarning($"Could not parse identifier {cleanIdentifier}.");
            headerType = (HeaderType)(-1);
            return false;
        }
        headerType = (HeaderType)enumObject;
        return true;
    }

    private static KeyValuePair<int, string> defaultKVP = new(-1, "Invalid event");
    public static bool TryParseChartLine(string line, out KeyValuePair<int, string> formattedKVP)
    {
        formattedKVP = defaultKVP;

        var parts = line.Split(" = ", 2);
        if (!int.TryParse(parts[0].Trim(), out int tick))
        {
            Debug.LogWarning($"Problem parsing tick {parts[0].Trim()}");
            return false;
        }

        formattedKVP = new KeyValuePair<int, string>(tick, parts[1]);
        return true;
    }

    public static string CreateHeader(int headerID) => CreateHeader((HeaderType)headerID);

    public static InstrumentCategory GetInstrumentGroup(HeaderType headerType)
    {
        return (int)headerType switch
        {
            < 10 => InstrumentCategory.None,
            < 100 => InstrumentCategory.FiveFret,
            < 110 => InstrumentCategory.FourLaneDrums,
            < 120 => InstrumentCategory.EliteDrums,
            < 10000 => InstrumentCategory.GHL,
            < 100000 => InstrumentCategory.Vox,
            _ => throw new ArgumentException("Tried to get invalid instrument group.")
        };
    }

    public static DifficultyType GetDifficulty(HeaderType instrumentID)
    {
        return ((int)instrumentID % 10) switch
        {
            0 => DifficultyType.easy,
            1 => DifficultyType.medium,
            2 => DifficultyType.hard,
            3 => DifficultyType.expert,
            _ => throw new ArgumentException("Tried to get invalid instrument difficulty.")
        };
    }

    public static string GetDifficultyAbbreviation(DifficultyType difficultyType)
    {
        return (difficultyType) switch
        {
            DifficultyType.easy => "E",
            DifficultyType.medium => "M",
            DifficultyType.hard => "H",
            DifficultyType.expert => "X",
            _ => throw new ArgumentException("Tried to get invalid instrument difficulty string.")
        };
    }

    public static string GetDifficultyAbbreviation(HeaderType header) => GetDifficultyAbbreviation(GetDifficulty(header));

    public static InstrumentType GetInstrumentType(HeaderType instrumentID)
    {
        return ((int)instrumentID) switch
        {
            1 => InstrumentType.synctrack,
            < 20 => InstrumentType.guitar,
            < 30 => InstrumentType.coopGuitar,
            < 40 => InstrumentType.bass,
            < 50 => InstrumentType.rhythm,
            < 60 => InstrumentType.keys,
            < 110 => InstrumentType.drums,
            < 120 => InstrumentType.eliteDrums,
            < 1010 => InstrumentType.ghlGuitar,
            < 1020 => InstrumentType.ghlBass,
            < 1030 => InstrumentType.ghlCoop,
            < 1040 => InstrumentType.ghlRhythm,
            < 10010 => InstrumentType.vox,
            < 10020 => InstrumentType.harmonies,
            _ => throw new ArgumentException(
            $"Tried to get invalid/uninitialized instrument type." +
            $" Please ensure that InstrumentType, InstrumentCategory, " +
            $"HeaderType, and InstrumentMetadata (and InstrumentIconMatchup) " +
            $"are properly updated to support your new instrument. " +
            $"Check Scripts/Core/CommonTypes.cs for more information.")
        };
    }
}

public enum SceneType
{
    setup,
    tempoMap,
    fiveFretChart,
    starpower,
}