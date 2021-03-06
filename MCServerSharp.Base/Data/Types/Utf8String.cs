﻿using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MCServerSharp.Collections;

namespace MCServerSharp
{
    // TODO: do stuff with JsonEncodedText and such
    // TODO: overload + operator to more efficiently combine Utf8String and String instances

    [DebuggerDisplay("{ToString()}")]
    public class Utf8String : IComparable<Utf8String>, IEquatable<Utf8String>, ILongHashable
    {
        public static Utf8String Empty { get; } = new Utf8String(Array.Empty<byte>());

        private readonly byte[] _bytes;

        public ReadOnlySpan<byte> Bytes => _bytes;
        public int Length => _bytes.Length;

        #region Constructors

        private Utf8String(byte[] bytes)
        {
            _bytes = bytes;
        }

        private Utf8String(int length) : this(length == 0 ? Empty._bytes : new byte[length])
        {
        }

        public Utf8String(string value) : this(StringHelper.Utf8.GetByteCount(value))
        {
            StringHelper.Utf8.GetBytes(value, _bytes);
        }

        public Utf8String(ReadOnlySpan<char> chars) : this(StringHelper.Utf8.GetByteCount(chars))
        {
            StringHelper.Utf8.GetBytes(chars, _bytes);
        }

        public Utf8String(ReadOnlySpan<byte> bytes) : this(bytes.Length)
        {
            bytes.CopyTo(_bytes);
        }

        #endregion

        public static Utf8String Create<TState>(
            int length, TState state, SpanAction<byte, TState> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (length == 0)
                return Empty;

            var str = new Utf8String(length);
            action.Invoke(str._bytes, state);
            return str;
        }

        public static bool IsNullOrEmpty([NotNullWhen(false)] Utf8String? value)
        {
            return value == null || value.Length == 0;
        }

        public int CompareTo(Utf8String? other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other == null)
                return 1;

            return Bytes.SequenceCompareTo(other.Bytes);
        }

        public bool Equals(Utf8String? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other == null)
                return false;

            return Length == other.Length
                && Bytes.SequenceEqual(other.Bytes);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Utf8String);
        }

        /// <summary>
        /// Constructs a new <see cref="string"/> from this <see cref="Utf8String"/>.
        /// </summary>
        public override string ToString()
        {
            return StringHelper.Utf8.GetString(Bytes);
        }

        // TODO: possibly optimize with interning

        [return: NotNullIfNotNull("value")]
        public static explicit operator string?(Utf8String? value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        [return: NotNullIfNotNull("value")]
        public static explicit operator Utf8String?(string? value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return Empty;

            return new Utf8String(value);
        }

        [return: NotNullIfNotNull("value")]
        public static Utf8String? ToUtf8String(string? value)
        {
            return (Utf8String?)value;
        }

        public override int GetHashCode()
        {
            return LongEqualityComparer<Utf8String>.Default.GetHashCode(this);
        }

        public long GetLongHashCode()
        {
            return LongEqualityComparer<Utf8String>.Default.GetLongHashCode(this);
        }

        public static bool operator ==(Utf8String? left, Utf8String? right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Utf8String? left, Utf8String? right)
        {
            return !(left == right);
        }

        public static bool operator <(Utf8String? left, Utf8String? right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Utf8String? left, Utf8String? right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Utf8String? left, Utf8String? right)
        {
            return left is object && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Utf8String? left, Utf8String? right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
