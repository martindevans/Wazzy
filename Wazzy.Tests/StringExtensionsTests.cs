using Wazzy.Extensions;

namespace Wazzy.Tests;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    public void TrimStart_Trim()
    {
        var str = "abc123";
        var result = str.TrimStart("abc");

        Assert.AreEqual("123", result);
    }

    [TestMethod]
    public void TrimStart_Nothing()
    {
        var str = "abc123";
        var result = str.TrimStart("123");

        Assert.AreEqual("abc123", result);
    }
}