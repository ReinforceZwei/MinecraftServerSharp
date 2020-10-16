﻿using System;
using System.Runtime.InteropServices;

namespace MCServerSharp.Collections
{
    internal sealed class LongStringComparer : LongEqualityComparer<string?>
    {
        public override bool IsRandomized => true;

        public override int GetHashCode(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var span = MemoryMarshal.AsBytes(value.AsSpan());
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash32(hash);
        }

        public override long GetLongHashCode(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var span = MemoryMarshal.AsBytes(value.AsSpan());
            var hash = MarvinHash64.ComputeHash(span, MarvinHash64.DefaultSeed);
            return MarvinHash64.CollapseHash64(hash);
        }
    }
}