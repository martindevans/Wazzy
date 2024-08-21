namespace Wazzy.Extensions;

internal static class SpanExtensions
{
    /// <summary>
    /// Split a span around the first item which is equal to `split`. e.g. "A_B_C" -> "A" and "B_C"
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span">Span to split</param>
    /// <param name="split">Item to split around</param>
    /// <param name="left">All items before `split`</param>
    /// <param name="right">All items after `split`</param>
    public static void Split<T>(this ReadOnlySpan<T> span, T split, out ReadOnlySpan<T> left, out ReadOnlySpan<T> right)
        where T : IEquatable<T>
    {
        var idx = span.IndexOf(split);

        if (idx < 0)
        {
            left = span;
            right = [];
        }
        else
        {
            left = span[..idx];
            right = span[(idx + 1)..];
        }
    }

    /// <summary>
    /// Split a span around the last item which is equal to `split`. e.g. "A_B_C" -> "A_B" and "C"
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span">Span to split</param>
    /// <param name="split">Item to split around</param>
    /// <param name="left">All items before `split`</param>
    /// <param name="right">All items after `split`</param>
    public static void SplitLast<T>(this ReadOnlySpan<T> span, T split, out ReadOnlySpan<T> left, out ReadOnlySpan<T> right)
        where T : IEquatable<T>
    {
        var idx = span.LastIndexOf(split);

        if (idx < 0)
        {
            left = span;
            right = [];
        }
        else
        {
            left = span[..idx];
            right = span[(idx + 1)..];
        }
    }
}