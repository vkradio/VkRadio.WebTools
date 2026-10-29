using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

/// <summary>
/// Abstract base class for exceptions during parse
/// </summary>
public abstract class WebPageParseException : WebGrabberException
{
    public WebPageParseException() : base()
    {
    }
    
    public WebPageParseException(string message) : base(message)
    {
    }
    
    public WebPageParseException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    ///  Raw HTML soup received from web server as is
    /// </summary>
    public string? RawHtmlSoup { get; set; }

    /// <summary>
    ///  If we have prepared some of HTML content to the step of the exception
    /// </summary>
    public string? PreparedHtmlAtThisStage { get; set; }
}
