using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

/// <summary>
///  Abstract base class of reading of a web page
/// </summary>
public abstract class WebPageReadException : WebGrabberException
{
    public WebPageReadException() : base()
    {
    }
    
    public WebPageReadException(string message) : base(message)
    {
    }
    
    public WebPageReadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
