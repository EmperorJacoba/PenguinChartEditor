using System;
using System.Collections.Generic;

public interface IEventData : IEquatable<IEventData>
{
    string[] ToChartFormat(int lane);
}

public interface ISustainable
{
    int Sustain { get; set; }
    ISustainable ExportWithNewSustain(int newSustain);
}

public struct BPMData : IEquatable<BPMData>, IEventData
{
    public const int BPM_CONVERSION = 1000;
    private const string BPM_IDENTIFIER = "B";

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
    public bool Equals(IEventData data) => data is BPMData other && Equals(other);
    public bool Equals(BPMData other) => BPMChange == other.BPMChange && Timestamp == other.Timestamp;

    public override string ToString() => $"{BPMChange} @ {Timestamp}s, Anchor = {Anchor}";

    public string[] ToChartFormat(int lane) => new string[1] { $"{BPM_IDENTIFIER} {BPMChange * BPM_CONVERSION}" };

    public override int GetHashCode()
    {
        return HashCode.Combine(BPMChange, Timestamp, Anchor);
    }
}

public struct TSData : IEquatable<TSData>, IEventData
{
    private const string TS_IDENTIFIER = "TS";

    public int Numerator;
    public int Denominator;

    public TSData(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public override bool Equals(object obj) => obj is TSData other && Equals(other);
    public bool Equals(IEventData data) => data is TSData other && Equals(other);
    public bool Equals(TSData other) => Numerator == other.Numerator && Denominator == other.Denominator;

    public override string ToString() => $"{Numerator} / {Denominator}";

    public string[] ToChartFormat(int lane) 
    {
        string denom;

        if (Denominator == 4) denom = "";
        else denom = $" {Math.Log(Denominator, 2)}";

        return new string[1] { $"{TS_IDENTIFIER} {Numerator}{denom}" }; // denom will contain leading space if needed
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    public static bool operator ==(TSData left, TSData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TSData left, TSData right)
    {
        return !(left == right);
    }
}

// Note datas: LaneType is an enum with lane corresponding to their ID number in .chart files.
// FlagType is an enum with flag corresponding to ID number in .chart files 

public struct FiveFretNoteData : IEventData, ISustainable, IEquatable<FiveFretNoteData>
{
    public enum FlagType
    {
        strum = 4,
        hopo = 5,
        tap = 6
    }

    // true = flip as needed, false = hold Flag no matter what
    public bool Default; 
    public int Sustain { get; set; }
    public FlagType Flag;

    public FiveFretNoteData(int sustain, FlagType flag, bool defaultOrientation = true)
    {
        Sustain = sustain;
        Flag = flag;
        Default = defaultOrientation;
    }


    public override string ToString() => $"(FFN: {Flag}, defaultOrientation = {Default}. {Sustain}T sustain)";
    public string[] ToChartFormat(int lane)
    {
        int laneIdentifier = FiveFretInstrument.MatchLaneOrientationToChartID((FiveFretInstrument.LaneOrientation)lane);
        return new string[1] { $"N {laneIdentifier} {Sustain}" };
    }

    public FiveFretNoteData ExportWithNewFlag(FlagType newFlag)
    {
        return new FiveFretNoteData(Sustain, newFlag, Default);
    }

    public FiveFretNoteData ExportWithNewSustain(int newSustain)
    {
        return new FiveFretNoteData(newSustain, Flag, Default);
    }
    ISustainable ISustainable.ExportWithNewSustain(int newSustain) => ExportWithNewSustain(newSustain);

    public FiveFretNoteData ExportWithNewDefault(bool state)
    {
        return new FiveFretNoteData(Sustain, Flag, state);
    }

    public bool Equals(IEventData other) => other is FiveFretNoteData data && Equals(data);

    public bool Equals(FiveFretNoteData other)
    {
        return Default == other.Default &&
               Sustain == other.Sustain &&
               Flag == other.Flag;
    }

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode()
    {
        return HashCode.Combine(Default, Sustain, Flag);
    }

    public static bool operator ==(FiveFretNoteData left, FiveFretNoteData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FiveFretNoteData left, FiveFretNoteData right)
    {
        return !(left == right);
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

    public string[] ToChartFormat(int lane)
    {
        throw new NotImplementedException();
    }

    public bool Equals(IEventData other)
    {
        throw new NotImplementedException();
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
    public string[] ToChartFormat(int lane)
    {
        throw new NotImplementedException();
    }

    public bool Equals(IEventData other)
    {
        throw new NotImplementedException();
    }
}

/*
public struct TrueDrumNoteData : IEventData
{
    // implement when the time comes
}

public struct VoxData : IEventData
{

} */

public struct StarpowerEventData : IEventData, IEquatable<StarpowerEventData>, ISustainable
{
    public bool IsFill;
    public int Sustain { get; set; }

    public StarpowerEventData(bool isFill, int sustain)
    {
        IsFill = isFill;
        Sustain = sustain;
    }

    public string[] ToChartFormat(int lane)
    {
        string @event = "";
        if (IsFill)
        {
            @event = $"S 64 {Sustain}";
        }
        else
        {
            @event = $"S 2 {Sustain}";
        }
        return new string[1] { @event };
    }

    ISustainable ISustainable.ExportWithNewSustain(int newSustain) => ExportWithNewSustain(newSustain);

    public StarpowerEventData ExportWithNewSustain(int newSustain)
    {
        return new(IsFill, newSustain);
    }

    public bool Equals(StarpowerEventData other)
    {
        return 
           IsFill == other.IsFill &&
           Sustain == other.Sustain;
    }

    public bool Equals(IEventData other)
    {
        return other is StarpowerEventData data &&
               IsFill == data.IsFill &&
               Sustain == data.Sustain;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsFill, Sustain);
    }

    public override bool Equals(object obj)
    {
        return obj is StarpowerEventData data &&
               IsFill == data.IsFill &&
               Sustain == data.Sustain;
    }

    public static bool operator ==(StarpowerEventData left, StarpowerEventData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StarpowerEventData left, StarpowerEventData right)
    {
        return !(left == right);
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
    public string[] ToChartFormat(int lane)
    {
        throw new NotImplementedException();
    }

}

public struct SoloEventData : IEventData, IEquatable<SoloEventData>
{
    public int StartTick;

    public int EndTick
    {
        get
        {
            // Allows setting a general open ended end tick. Used during pre-audio initialization.
            if (_etick != int.MaxValue) return _etick;
            return StartTick - SongTime.SongLengthTicks;
        }
        set
        {
            _etick = value;
        }
    }

    private int _etick;

    public SoloEventData(int startTick, int endTick)
    {
        StartTick = startTick;
        _etick = endTick;
    }

    public SoloEventData(int startTick)
    {
        StartTick = startTick;
        _etick = int.MaxValue;
    }

    public override bool Equals(object obj)
    {
        return obj is SoloEventData data && Equals(data);
    }

    public bool Equals(IEventData data) => Equals((object)data);

    public bool Equals(SoloEventData other)
    {
        return StartTick == other.StartTick &&
               EndTick == other.EndTick;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartTick, EndTick);
    }

    public static bool operator ==(SoloEventData left, SoloEventData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SoloEventData left, SoloEventData right)
    {
        return !(left == right);
    }

    public string[] ToChartFormat(int lane)
    {
        return new string[2]
        {
            $"{StartTick} = E solo",
            $"{EndTick} = E soloend"
        };
    }
}