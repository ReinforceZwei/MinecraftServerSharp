﻿using System;

namespace MCServerSharp.Maths
{
    public struct ChunkPosition : IEquatable<ChunkPosition>, ILongHashable
    {
        public int X;
        public int Z;

        public ChunkPosition(int x, int z)
        {
            X = x;
            Z = z;
        }

        public readonly long ToLong()
        {
            return (long)X << 32 | (long)Z;
        }

        public static double Dot(ChunkPosition a, ChunkPosition b)
        {
            return (a.X * b.X) + (a.Z * b.Z);
        }

        public static double DistanceSquared(ChunkPosition a, ChunkPosition b)
        {
            var difference = a - b;
            return Dot(difference, difference);
        }

        public static ChunkPosition operator +(ChunkPosition a, ChunkPosition b)
        {
            return new ChunkPosition(
                a.X + b.X,
                a.Z + b.Z);
        }

        public static ChunkPosition operator -(ChunkPosition left, ChunkPosition right)
        {
            return new ChunkPosition(
                left.X - right.X,
                left.Z - right.Z);
        }

        public readonly bool Equals(ChunkPosition other)
        {
            return this == other;
        }

        public readonly long GetLongHashCode()
        {
            return ToLong();
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ChunkPosition value && Equals(value);
        }

        public override readonly string ToString()
        {
            return "{X:" + X + ", Z:" + Z + "}";
        }

        public static bool operator ==(ChunkPosition a, ChunkPosition b)
        {
            return a.X == b.X
                && a.Z == b.Z;
        }

        public static bool operator !=(ChunkPosition a, ChunkPosition b)
        {
            return !(a == b);
        }
    }
}
