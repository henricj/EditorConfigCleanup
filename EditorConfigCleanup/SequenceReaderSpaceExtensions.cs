using System.Buffers;

namespace EditorConfigCleanup;

public static class SequenceReaderSpaceExtensions
{
    public static readonly byte[] Space = [(byte)' ', (byte)'\t'];

    public static long AdvancePastSpace(this ref SequenceReader<byte> reader)
    {
        return reader.AdvancePastAny((byte)' ', (byte)'\t');
    }
}
