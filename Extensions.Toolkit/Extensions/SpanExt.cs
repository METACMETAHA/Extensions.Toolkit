using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Extensions.Toolkit;

public static class SpanExt
{
    public readonly ref struct EnumerableSingleSeparator<T> where T : IEquatable<T>
    {
        public EnumerableSingleSeparator(ReadOnlySpan<T> span, T separator)
        {
            Span = span;
            Separator = separator;
        }

        private ReadOnlySpan<T> Span { get; }
        private T Separator { get; }

        public EnumeratorSingleSeparator<T> GetEnumerator() => new(Span, Separator);

        public ReadOnlySpan<T> Last()
        {
            EnumeratorSingleSeparator<T> e = GetEnumerator();
            if (e.MoveNext()) 
            {
                ReadOnlySpan<T> result;
                do {
                    result = e.Current;
                } while (e.MoveNext());
                return result;
            }
            throw new InvalidOperationException("Sequence contains no elements");
        }
    }

    public readonly ref struct EnumerableMultipleSeparators<T> where T : IEquatable<T>
    {
        public EnumerableMultipleSeparators(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
        {
            Span = span;
            Separators = separators;
        }

        private ReadOnlySpan<T> Span { get; }
        private ReadOnlySpan<T> Separators { get; }

        public EnumeratorMultipleSeparators<T> GetEnumerator() => new(Span, Separators);
        
        public ReadOnlySpan<T> Last()
        {
            EnumeratorMultipleSeparators<T> e = GetEnumerator();
            if (e.MoveNext()) 
            {
                ReadOnlySpan<T> result;
                do {
                    result = e.Current;
                } while (e.MoveNext());
                return result;
            }
            throw new InvalidOperationException("Sequence contains no elements");
        }
    }

    public ref struct EnumeratorSingleSeparator<T> where T : IEquatable<T>
    {
        public EnumeratorSingleSeparator(ReadOnlySpan<T> span, T separator)
        {
            Span = span;
            Separator = separator;
            Current = default;

            if (Span.IsEmpty)
                TrailingEmptyItem = true;
        }

        private ReadOnlySpan<T> Span { get; set; }
        public ReadOnlySpan<T> Current { get; private set; }
        private T Separator { get; }
        private static int SeparatorLength => 1;

        private ReadOnlySpan<T> TrailingEmptyItemSentinel => Unsafe.As<T[]>(nameof(TrailingEmptyItemSentinel)).AsSpan();

        private bool TrailingEmptyItem
        {
            get => Span == TrailingEmptyItemSentinel;
            set => Span = value ? TrailingEmptyItemSentinel : default;
        }

        public bool MoveNext()
        {
            if (TrailingEmptyItem)
            {
                TrailingEmptyItem = false;
                Current = default;
                return true;
            }

            if (Span.IsEmpty)
            {
                Span = Current = default;
                return false;
            }

            int idx = Span.IndexOf(Separator);
            if (idx < 0)
            {
                Current = Span;
                Span = default;
            }
            else
            {
                Current = Span[..idx];
                Span = Span[(idx + SeparatorLength)..];
                if (Span.IsEmpty)
                    TrailingEmptyItem = true;
            }

            return true;
        }
    }

    public ref struct EnumeratorMultipleSeparators<T> where T : IEquatable<T>
    {
        public EnumeratorMultipleSeparators(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
        {
            Span = span;
            Separators = separators;
            Current = default;

            if (Span.IsEmpty)
                TrailingEmptyItem = true;
        }

        private ReadOnlySpan<T> Span { get; set; }
        private ReadOnlySpan<T> Separators { get; }
        public ReadOnlySpan<T> Current { get; private set; }
        private static int SeparatorLength => 1;

        private ReadOnlySpan<T> TrailingEmptyItemSentinel => Unsafe.As<T[]>(nameof(TrailingEmptyItemSentinel)).AsSpan();

        private bool TrailingEmptyItem
        {
            get => Span == TrailingEmptyItemSentinel;
            set => Span = value ? TrailingEmptyItemSentinel : default;
        }

        public bool MoveNext()
        {
            if (TrailingEmptyItem)
            {
                TrailingEmptyItem = false;
                Current = default;
                return true;
            }

            if (Span.IsEmpty)
            {
                Span = Current = default;
                return false;
            }

            int idx = Span.IndexOfAny(Separators);
            if (idx < 0)
            {
                Current = Span;
                Span = default;
            }
            else
            {
                Current = Span[..idx];
                Span = Span[(idx + SeparatorLength)..];
                if (Span.IsEmpty)
                    TrailingEmptyItem = true;
            }

            return true;
        }
    }

    [Pure]
    public static EnumerableSingleSeparator<T> Split<T>(this ReadOnlySpan<T> span, T separator)
        where T : IEquatable<T> => new(span, separator);

    [Pure]
    public static EnumerableMultipleSeparators<T> Split<T>(this ReadOnlySpan<T> span, T[] separators)
        where T : IEquatable<T> => new(span, separators.AsSpan());
}