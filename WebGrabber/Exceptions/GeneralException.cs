using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class GeneralException : WebPageReadException
{
    public GeneralException() : base()
    {
    }
    
    public GeneralException(string message) : base(message)
    {
    }
    
    public GeneralException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
