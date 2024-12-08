namespace EditorConfigCleanup;

public abstract class Line
{
    public static readonly Line Empty = new EmptyLine();
}

public sealed class EmptyLine : Line
{
    public override string ToString()
    {
        return string.Empty;
    }
}

public sealed class CommentLine(string comment) : Line
{
    public string Comment { get; } = comment;

    public override string ToString()
    {
        return Comment;
    }
}

public sealed class SectionLine(string section) : Line
{
    public string Section { get; } = section;

    public override string ToString()
    {
        return $"[{Section}]";
    }
}

public sealed class PropertyLine(string key, string value) : Line
{
    public string Key { get; } = key;
    public string Value { get; } = value;

    public override string ToString()
    {
        return $"{Key} = {Value}";
    }
}
