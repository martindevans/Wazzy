using Wazzy.Extensions;

namespace Wazzy.Tests;

[TestClass]
public class SpanExtensionsTests
{
    [TestMethod]
    public void SplitSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.Split('c', out var left, out var right);

        Assert.AreEqual(string.Join("", left.ToArray()), "ab");
        Assert.AreEqual(string.Join("", right.ToArray()), "dabcd");
    }

    [TestMethod]
    public void NoSplitSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.Split('e', out var left, out var right);

        Assert.AreEqual(string.Join("", left.ToArray()), "abcdabcd");
        Assert.AreEqual(string.Join("", right.ToArray()), "");
    }

    [TestMethod]
    public void SplitLastSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.SplitLast('c', out var left, out var right);

        Assert.AreEqual(string.Join("", left.ToArray()), "abcdab");
        Assert.AreEqual(string.Join("", right.ToArray()), "d");
    }

    [TestMethod]
    public void NoSplitLastSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.SplitLast('e', out var left, out var right);

        Assert.AreEqual(string.Join("", left.ToArray()), "abcdabcd");
        Assert.AreEqual(string.Join("", right.ToArray()), "");
    }
}