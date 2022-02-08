using System;
using System.ComponentModel.DataAnnotations;

namespace LogGrokCore.Data;

public readonly struct StringRange : IEquatable<StringRange>
{
    public string SourceString { get; init; }

    public int Start { get; init; }

    public int Length { get; init; }
    
    public int End => Start + Length;

    public ReadOnlySpan<char> Span => SourceString.AsSpan(Start, Length);

    public override string ToString()
    {
        return new string(Span);
    }

    public bool IsEmpty => Length == 0;
    
    public static StringRange Empty = new() { SourceString = string.Empty, Start = 0, Length = 0 };

    
    public static StringRange FromString(string source)
    {
        return new StringRange()
        {
            SourceString = source, Length = source.Length, Start = 0
        };
    }

    public bool Equals(StringRange other)
    {
        return Span.SequenceEqual(other.Span);
    }

    public override bool Equals(object? obj)
    {
        return obj is StringRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return string.GetHashCode(Span);
    }
}