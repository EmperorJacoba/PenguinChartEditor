using System;
using System.Collections.Generic;

public interface IEventData {}

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

// Note datas: LaneType is an enum with lane corresponding to their ID number in .chart files.
// FlagType is an enum with flag corresponding to ID number in .chart files 

public struct FiveFretNoteData : IEventData
{
    public enum FlagType
    {
        forced = 5,
        tap = 6
    }

    public int Sustain;
    public List<FlagType> Flags;

    public FiveFretNoteData(int sustain, List<FlagType> flags)
    {
        Sustain = sustain;
        Flags = flags;
    }
}

public struct FourLaneDrumNoteData : IEventData
{
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
    public List<FlagType> Flags;

    public FourLaneDrumNoteData(List<FlagType> flags)
    {
        Flags = flags;
    }
}

public struct GHLNoteData : IEventData
{
    public enum FlagType
    {
        forced = 5,
        tap = 6
    }

    public List<FlagType> Flags;

    public GHLNoteData(List<FlagType> flags)
    {
        Flags = flags;
    }
}

public struct TrueDrumNoteData : IEventData
{
    // implement when the time comes
}

public struct StarpowerData
{

}