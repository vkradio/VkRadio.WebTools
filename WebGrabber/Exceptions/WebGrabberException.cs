using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

/// <summary>
///  Abstract base class for all WebGrabber exceptions
/// </summary>
public abstract class WebGrabberException : ApplicationException
{
    public WebGrabberException() : base()
    {
    }
    
    public WebGrabberException(string message) : base(message)
    {
    }
    
    public WebGrabberException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
