using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace EditorConfigCleanup;

static class EditorConfigReader
{
    private const byte Lf = (byte)'\n';
    private const byte Cr = (byte)'\r';

    public static async IAsyncEnumerable<Line> ReadAsync(PipeReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var lines = new List<Line>();
        var lineNumber = 0;

        for (;;)
        {
            if (!reader.TryRead(out var result))
                result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            var isCompleted = result.IsCompleted;
            if (result.IsCanceled)
                throw new OperationCanceledException();

            // Scope
            {
                SequenceReader<byte> sequenceReader = new(result.Buffer);
                while (sequenceReader.TryReadTo(out ReadOnlySequence<byte> line, Lf))
                {
                    ++lineNumber;

                    line = StripCr(line);

                    lines.Add(CreateLine(lineNumber, line));
                }

                reader.AdvanceTo(sequenceReader.Position, result.Buffer.End);
            }

            foreach (var line in lines)
                yield return line;

            lines.Clear();

            if (isCompleted)
            { // TODO: Handle the last line
                break;
            }
        }
    }

    private static ReadOnlySequence<byte> StripCr(ReadOnlySequence<byte> line)
    {
        if (line.IsEmpty)
            return line;

        var last = line.Slice(line.GetPosition(line.Length - 1), line.End);
        Debug.Assert(last.IsSingleSegment); // It is only one character long, so it has to be a single segment, right?
        Debug.Assert(last is { IsEmpty: false, FirstSpan.IsEmpty: false });

        if (last.FirstSpan[0] == Cr)
            line = line.Slice(0, line.Length - 1);

        return line;
    }


    private static Line CreateLine(int lineNumber, ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
            return Line.Empty;

        var reader = new SequenceReader<byte>(sequence);

        reader.AdvancePastSpace();
        if (!reader.TryPeek(out var first))
            return Line.Empty;

        switch (first)
        {
            case (byte)'#':
            case (byte)';':
            {
                var comment = reader.UnreadSequence.TrimEnd();
                return new CommentLine(Encoding.UTF8.GetString(comment));
            }

            case (byte)'[':
            {
                reader.Advance(1);
                if (!reader.TryReadTo(out ReadOnlySequence<byte> section, (byte)']'))
                    throw new FormatException($"Invalid section \"{Encoding.UTF8.GetString(sequence)}\" ");

                return new SectionLine(Encoding.UTF8.GetString(section.Trim()));
            }

            default:
            {
                if (!reader.TryReadTo(out ReadOnlySequence<byte> key, (byte)'='))
                    throw new FormatException($"Invalid property \"{Encoding.UTF8.GetString(sequence)}\" ");

                var keyString = Encoding.UTF8.GetString(key.Trim());
                var valueString = Encoding.UTF8.GetString(reader.UnreadSequence.Trim());

                return new PropertyLine(keyString, valueString);
            }
        }
    }
}
