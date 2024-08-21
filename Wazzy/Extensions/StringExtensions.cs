namespace Wazzy.Extensions;

internal static class StringExtensions
{
    public static string TrimStart(this string text, string prefix)
    {
        if (!text.StartsWith(prefix))
            return text;

        return text[prefix.Length..];
    }
}