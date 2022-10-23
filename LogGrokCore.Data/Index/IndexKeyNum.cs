using System;

namespace LogGrokCore.Data.Index;

public readonly struct IndexKeyNum : IEquatable<IndexKeyNum>, IComparable<IndexKeyNum>
{
    public int KeyNum { get; init; }

    public bool Equals(IndexKeyNum other)
    {
        return KeyNum == other.KeyNum;
    }

    public override bool Equals(object? obj)
    {
        return obj is IndexKeyNum other && Equals(other);
    }

    public override int GetHashCode()
    {
        return KeyNum.GetHashCode();
    }

    public int CompareTo(IndexKeyNum other)
    {
        return KeyNum.CompareTo(other.KeyNum);
    }
}