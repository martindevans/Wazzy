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

        Assert.AreEqual("ab", string.Join("", left.ToArray()));
        Assert.AreEqual("dabcd", string.Join("", right.ToArray()));
    }

    [TestMethod]
    public void NoSplitSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.Split('e', out var left, out var right);

        Assert.AreEqual("abcdabcd", string.Join("", left.ToArray()));
        Assert.AreEqual("", string.Join("", right.ToArray()));
    }

    [TestMethod]
    public void SplitLastSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.SplitLast('c', out var left, out var right);

        Assert.AreEqual("abcdab", string.Join("", left.ToArray()));
        Assert.AreEqual("d", string.Join("", right.ToArray()));
    }

    [TestMethod]
    public void NoSplitLastSpan()
    {
        ReadOnlySpan<char> span = "abcdabcd";

        span.SplitLast('e', out var left, out var right);

        Assert.AreEqual("abcdabcd", string.Join("", left.ToArray()));
        Assert.AreEqual("", string.Join("", right.ToArray()));
    }
}