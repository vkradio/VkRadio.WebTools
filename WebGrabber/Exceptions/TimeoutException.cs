using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class TimeoutException : WebPageReadException
{
    public TimeoutException() : base()
    {
    }
    
    public TimeoutException(string message) : base(message)
    {
    }
    
    public TimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
