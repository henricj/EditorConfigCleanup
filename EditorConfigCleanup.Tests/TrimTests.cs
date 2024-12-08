using System.Buffers;
using System.Text;

namespace EditorConfigCleanup.Tests;

[TestClass]
public sealed class TrimTests
{
    [TestMethod]
    public void Empty()
    {
        var sequence = new ReadOnlySequence<byte>([]);

        var actual = sequence.Trim();

        Assert.AreEqual(sequence, actual);
    }

    [TestMethod]
    public void OneWord()
    {
        var sequence = new ReadOnlySequence<byte>("one"u8.ToArray());

        var actual = sequence.Trim();

        Assert.AreEqual(sequence, actual);
    }

    [TestMethod]
    [DataRow("Hello", "Hello")]
    [DataRow("Hello ", "Hello ")]
    [DataRow("Hello  ", "Hello  ")]
    [DataRow("Hello", " Hello")]
    [DataRow("Hello", "  Hello")]
    [DataRow("Hello ", " Hello ")]
    [DataRow("Hello  ", "  Hello  ")]
    public void TrimStart(string expectedString, string value)
    {
        var expected = GetSequence(expectedString);
        var sequence = GetSequence(value);

        var actual = sequence.TrimStart();

        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
    }

    [TestMethod]
    [DataRow("Hello", "Hello")]
    [DataRow("Hello", "Hello ")]
    [DataRow("Hello", "Hello  ")]
    [DataRow(" Hello", " Hello")]
    [DataRow(" Hello", " Hello ")]
    [DataRow(" Hello", " Hello  ")]
    public void TrimEnd(string expectedString, string value)
    {
        var expected = GetSequence(expectedString);
        var sequence = GetSequence(value);

        var actual = sequence.TrimEnd();

        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
    }

    [TestMethod]
    [DataRow("Hello", "Hello")]
    [DataRow("Hello", "Hello ")]
    [DataRow("Hello", " Hello")]
    [DataRow("Hello", " Hello ")]
    public void Trim(string expectedString, string value)
    {
        var expected = GetSequence(expectedString);
        var sequence = GetSequence(value);

        var actual = sequence.Trim();

        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
    }

    private static ReadOnlySequence<byte> GetSequence(string value)
    {
        return new(Encoding.UTF8.GetBytes(value));
    }
}
