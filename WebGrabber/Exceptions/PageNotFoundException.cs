using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class PageNotFoundException : WebPageReadException
{
    public PageNotFoundException() : base()
    {
    }
    
    public PageNotFoundException(string message) : base(message)
    {
    }
    
    public PageNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
