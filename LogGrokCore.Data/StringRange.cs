﻿using System;

namespace LogGrokCore.Data;

public readonly struct StringRange
{
    public string SourceString { get; init; }

    public int Start { get; init; }

    public int Length { get; init; }

    public ReadOnlySpan<char> Span => SourceString.AsSpan(Start, Length);

    public override string ToString()
    {
        return new string(Span);
    }
}