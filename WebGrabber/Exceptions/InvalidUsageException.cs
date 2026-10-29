using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class InvalidUsageException : WebGrabberException
{
    public InvalidUsageException() : base()
    {
    }
    
    public InvalidUsageException(string message) : base(message)
    {
    }

    public InvalidUsageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
