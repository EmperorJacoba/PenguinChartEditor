using System;
using System.Collections.Generic;

public interface IEventData {}
public interface INoteData { }

public struct BPMData : IEquatable<BPMData>, IEventData
{
    public float BPMChange;
    public float Timestamp;

    public BPMData(float bpm, float timestamp)
    {
        BPMChange = bpm;
        Timestamp = timestamp;
    }

    public static bool operator !=(BPMData one, BPMData two)
    {
        return !one.Equals(two);
    }

    public static bool operator ==(BPMData one, BPMData two)
    {
        return one.Equals(two);
    }

    public override bool Equals(object obj)
    {
        return obj is BPMData other && Equals(other);
    }

    public bool Equals(BPMData other)
    {
        return BPMChange == other.BPMChange && Timestamp == other.Timestamp;
    }

    public override string ToString()
    {
        return $"{BPMChange}, {Timestamp}";
    }

    public override int GetHashCode() // literally just doing this because VSCode is yelling at me
    {
        unchecked
        {
            int hash = 17;
            hash *= 23 + BPMChange.GetHashCode();
            hash *= 23 + Timestamp.GetHashCode();
            return hash;
        }
    }
}

public struct TSData : IEquatable<TSData>, IEventData
{
    public int Numerator;
    public int Denominator;

    public TSData(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public override bool Equals(object obj)
    {
        return obj is TSData other && Equals(other);
    }
    public bool Equals(TSData other)
    {
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public override string ToString()
    {
        return $"{Numerator} / {Denominator}";
    }


    public override int GetHashCode() // literally just doing this because VSCode is yelling at me
    {
        unchecked
        {
            int hash = 17;
            hash *= 23 + Numerator.GetHashCode();
            hash *= 23 + Denominator.GetHashCode();
            return hash;
        }
    }
}

public struct BookmarkData : IEventData
{
    public string Name;

    public BookmarkData(string name)
    {
        Name = name;
    }
}

// class because note data should be modifiable 
public class InstrumentData<T> : IEventData where T : INoteData
{
    // int is direct counterpart to each NoteData's enum values 
    SortedDictionary<int, T> noteData;
}

// Note datas: LaneType is an enum with lane corresponding to their ID number in .chart files.
// FlagType is an enum with flag corresponding to ID number in .chart files 

public struct FiveFretNoteData : INoteData
{
    public enum LaneType
    {
        green = 0,
        red = 1,
        yellow = 2,
        blue = 3,
        orange = 4,
        open = 7
    }

    public enum FlagType
    {
        forced = 5,
        tap = 6
    }

    public LaneType Lane;
    public int Sustain;
    public List<FlagType> Flags;

    public FiveFretNoteData(LaneType lane, int sustain, List<FlagType> flags)
    {
        Lane = lane;
        Sustain = sustain;
        Flags = flags;
    }
    public FiveFretNoteData(LaneType lane, int sustain)
    {
        Lane = lane;
        Sustain = sustain;
        Flags = new();
    }

    public FiveFretNoteData(LaneType lane)
    {
        Lane = lane;
        Sustain = 0;
        Flags = new();
    }
}

public struct FourLaneDrumNoteData : INoteData
{
    public enum LaneType
    {
        red = 1,
        yellow = 2,
        blue = 3,
        green = 4,
        kick = 0,
        doubleKick = 32
    }

    public enum FlagType
    {
        accentRed = 34,
        accentYellow = 35,
        accentBlue = 36,
        accentGreen = 37,
        ghostRed = 40,
        ghostYellow = 41,
        ghostBlue = 42,
        ghostGreen = 43,
        cymbalYellow = 66,
        cymbalBlue = 67,
        cymbalGreen = 68
    }

    public LaneType Lane;
    public List<FlagType> Flags;

    public FourLaneDrumNoteData(LaneType lane, List<FlagType> flags)
    {
        Lane = lane;
        Flags = flags;
    }

    public FourLaneDrumNoteData(LaneType lane)
    {
        Lane = lane;
        Flags = new();
    }
}

public struct GHLNoteData : INoteData
{
    public enum LaneType
    {
        white1 = 0,
        white2 = 1,
        white3 = 2,
        black1 = 3,
        black2 = 4,
        black3 = 8,
        open = 7
    }

    public enum FlagType
    {
        forced = 5,
        tap = 6
    }

    public LaneType Lane;
    public List<FlagType> Flags;

    public GHLNoteData(LaneType lane, List<FlagType> flags)
    {
        Lane = lane;
        Flags = flags;
    }

    public GHLNoteData(LaneType lane)
    {
        Lane = lane;
        Flags = new();
    }
}

public struct TrueDrumNoteData : INoteData
{
    // implement when the time comes
}

public struct StarpowerData
{

}