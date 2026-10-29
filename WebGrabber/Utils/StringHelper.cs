using System.Net;

namespace VkRadio.WebTools.WebGrabber.Utils;

public static class StringHelper
{
    /// <summary>
    ///  Remove newline symbols, and convert HTML entities
    /// </summary>
    /// <param name="thisText"></param>
    /// <returns></returns>
    public static string ReconstructSingleLineAndStripHtmlEntities(this string thisText, bool trim = true)
    {
        var result = thisText
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");

        result = WebUtility.HtmlDecode(result);

        if (trim)
        {
            result = result.Trim();
        }

        return result;
    }
}
