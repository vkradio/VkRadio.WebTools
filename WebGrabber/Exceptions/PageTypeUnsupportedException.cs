using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class PageTypeUnsupportedException : WebPageParseException
{
    public PageTypeUnsupportedException() : base()
    {
    }
    
    public PageTypeUnsupportedException(string message) : base(message)
    {
    }
    
    public PageTypeUnsupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
