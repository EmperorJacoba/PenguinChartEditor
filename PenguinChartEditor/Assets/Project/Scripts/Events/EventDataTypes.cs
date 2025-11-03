using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEventData { }
public struct BPMData : IEquatable<BPMData>, IEventData
{
    public float BPMChange;
    public float Timestamp;
    public bool Anchor;
    public BPMData(float bpm, float timestamp, bool anchor)
    {
        BPMChange = bpm;
        Timestamp = timestamp;
        Anchor = anchor;
    }

    public static bool operator !=(BPMData one, BPMData two) => !one.Equals(two);

    public static bool operator ==(BPMData one, BPMData two) => one.Equals(two);

    public override bool Equals(object obj) => obj is BPMData other && Equals(other);

    public bool Equals(BPMData other) => BPMChange == other.BPMChange && Timestamp == other.Timestamp;

    public override string ToString() => $"({BPMChange}, {Timestamp})";

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

    public override bool Equals(object obj) => obj is TSData other && Equals(other);
    public bool Equals(TSData other) => Numerator == other.Numerator && Denominator == other.Denominator;

    public override string ToString() => $"({Numerator} / {Denominator})";

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
        strum = 4,
        hopo = 5,
        tap = 6
    }

    // true = flip as needed, false = hold Flag no matter what
    public bool Default; 
    public int Sustain;
    public FlagType Flag;

    public FiveFretNoteData(int sustain, FlagType flag, bool defaultOrientation = true)
    {
        Sustain = sustain;
        Flag = flag;
        Default = defaultOrientation;
    }

    public override string ToString() => $"(FFN: {Flag}, hold = {Default}. {Sustain}T sustain)";
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
        strum = 4,
        hopo = 5,
        tap = 6
    }

    public FlagType Flag;

    public GHLNoteData(int sustain, FlagType flag)
    {
        Flag = flag;
    }
}

public struct TrueDrumNoteData : IEventData
{
    // implement when the time comes
}

public struct VoxData : IEventData
{

}

public struct SpecialData
{
    public enum EventType
    {
        starpower = 2,
        drumFill = 64,
        drumRoll = 65,
        drumRollDouble = 66
    }

    public EventType eventType;
    public int Sustain;

    public SpecialData(int sustain, EventType eventType)
    {
        this.eventType = eventType;
        Sustain = sustain;
    }
}

public struct LocalEventData
{
    public enum EventType
    {
        solo,
        soloend
    }

    public EventType eventType;

    public LocalEventData(EventType eventType)
    {
        this.eventType = eventType;
    }
}