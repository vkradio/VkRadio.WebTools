using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class AccessDeniedException : WebPageReadException
{
    public AccessDeniedException() : base()
    {
    }
    
    public AccessDeniedException(string message) : base(message)
    {
    }
    
    public AccessDeniedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
