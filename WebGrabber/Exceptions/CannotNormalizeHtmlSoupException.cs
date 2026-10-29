using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

/// <summary>
/// Unable to adopt an HTML soup to an XML-combatible representation
/// </summary>
public class CannotNormalizeHtmlSoupException : WebPageParseException
{
    public CannotNormalizeHtmlSoupException() : base()
    {
    }
    
    public CannotNormalizeHtmlSoupException(string message) : base(message)
    {
    }
    
    public CannotNormalizeHtmlSoupException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
