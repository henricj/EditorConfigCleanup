using System.Buffers;
using System.Diagnostics;

namespace EditorConfigCleanup;

public static class ReadOnlySequenceTrimExtensions
{
    public static byte Last(this ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
            throw new InvalidOperationException("The sequence cannot be empty");

        var lastSlice = sequence.Slice(sequence.Length - 1);
        Debug.Assert(lastSlice.Length == 1);
        Debug.Assert(lastSlice is { IsSingleSegment: true, FirstSpan.IsEmpty: false });

        return lastSlice.FirstSpan[0];
    }

    public static SequencePosition LastNonSpace(this ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
            return sequence.Start;

        var lastValue = sequence.Last();
        if (!SequenceReaderSpaceExtensions.Space.Contains(lastValue))
            return sequence.End;

        var reader = new SequenceReader<byte>(sequence);
        var lastPosition = sequence.End;

        for (;;)
        {
            if (!reader.TryAdvanceToAny(SequenceReaderSpaceExtensions.Space, false))
                return lastPosition;

            lastPosition = reader.Position;

            reader.AdvancePastSpace();
        }
    }

    public static ReadOnlySequence<byte> TrimEnd(this ReadOnlySequence<byte> sequence)
    {
        var lastNonSpace = LastNonSpace(sequence);
        return sequence.Slice(sequence.Start, lastNonSpace);
    }

    public static ReadOnlySequence<byte> TrimStart(this ReadOnlySequence<byte> sequence)
    {
        var reader = new SequenceReader<byte>(sequence);
        reader.AdvancePastSpace();
        return reader.UnreadSequence;
    }

    public static ReadOnlySequence<byte> Trim(this ReadOnlySequence<byte> sequence)
    {
        return TrimEnd(TrimStart(sequence));
    }
}
