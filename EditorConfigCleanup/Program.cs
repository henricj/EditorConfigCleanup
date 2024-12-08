using System.Buffers.Text;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Text;
using EditorConfigCleanup;

const bool keepFirst = true;

using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;
Console.CancelKeyPress += (_, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

foreach (var filename in args)
{
    var sections = await ReadFileAsync(filename, cancellationToken).ConfigureAwait(false);

    await WriteFileAsync(filename, sections, cancellationToken).ConfigureAwait(false);
}

static async Task WriteFileAsync(string filename, List<(string section, List<Line> lines)> sections,
    CancellationToken cancellationToken)
{
    var path = Path.GetDirectoryName(filename);
    Span<byte> buffer = stackalloc byte[16];
    RandomNumberGenerator.Fill(buffer);
    var tempFile = Path.Combine(path ?? ".", $"{Base64Url.EncodeToString(buffer)}-{Path.GetFileName(filename)}.tmp");

    await using var stream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 32 * 1024,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

    var writer = new StreamWriter(stream);
    var sb = new StringBuilder();

    foreach (var (_, lines) in sections)
    {
        foreach (var line in lines)
        {
            sb.AppendLine(line.ToString());

            if (sb.Length < 32 * 1024)
                continue;

            await writer.WriteAsync(sb, cancellationToken).ConfigureAwait(false);
            sb.Clear();
        }
    }

    if (sb.Length > 0)
        await writer.WriteAsync(sb, cancellationToken).ConfigureAwait(false);

    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

    stream.Close();

    File.Move(tempFile, filename, true);
}

static async ValueTask<List<(string section, List<Line> lines)>> ReadFileAsync(string s, CancellationToken cancellationToken)
{
    {
        await using var stream = new FileStream(s, FileMode.Open, FileAccess.Read, FileShare.Read, 0,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        Console.WriteLine($"File: \"{s}\"");

        var reader = PipeReader.Create(stream, new(bufferSize: 32 * 1024));

        var valueTuples = new List<(string section, List<Line> lines)> { ("Global", []) };

        var global = valueTuples.First().lines;
        var section = global;

        var lineNumber = 0;
        await foreach (var line in EditorConfigReader.ReadAsync(reader, cancellationToken))
        {
            Console.WriteLine($"{++lineNumber,5}: {line}");

            switch (line)
            {
                case SectionLine sectionLine:
                    section = GetOrCreateSection(valueTuples, sectionLine);
                    continue;
                case PropertyLine propertyLine:
                    AddOrOverrideProperty(section, propertyLine);
                    continue;
                default:
                    section.Add(line);
                    break;
            }
        }

        return valueTuples;
    }

    static List<Line> GetOrCreateSection(List<(string section, List<Line> lines)> sections, SectionLine section)
    {
        for (var i = 0; i < sections.Count; ++i)
        {
            if (string.Equals(sections[i].section, section.Section, StringComparison.OrdinalIgnoreCase))
            {
                return sections[i].lines;
            }
        }

        var lines = new List<Line> { section };
        sections.Add((section.Section, lines));

        return lines;
    }

    static void AddOrOverrideProperty(List<Line> section, PropertyLine property)
    {
        for (var i = 0; i < section.Count; ++i)
        {
            var line = section[i];
            if (line is not PropertyLine propertyLine)
                continue;

            if (!string.Equals(propertyLine.Key, property.Key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (keepFirst)
            {
                section[i] = property;
                return;
            }

            section.RemoveAt(i);
            break;
        }

        section.Add(property);
    }
}
