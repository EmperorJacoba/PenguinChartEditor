using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This interface serves to group event data (e.g BPMData) for functions like clipboards/selection/etc.
/// </summary>
public interface IEventData {}

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

    public SortedDictionary<int, TSData> GetParentEventSet<T>()
    {
        return TimeSignature.Events;
    }
}

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

    public SortedDictionary<int, BPMData> GetParentEventSet<T>()
    {
        return BPM.Events;
    }
}

public struct SectionData : IEventData
{
    public string Text;
    public bool Local;
    public SectionData(string text, bool local = true)
    {
        Text = text;
        Local = local;
    }

    public SortedDictionary<int, SectionData> GetParentEventSet<T>()
    {
        throw new NotImplementedException();
    }
}