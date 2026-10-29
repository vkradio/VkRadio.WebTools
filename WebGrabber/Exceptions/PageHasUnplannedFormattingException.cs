using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class PageHasUnplannedFormattingException : WebPageParseException
{
    public PageHasUnplannedFormattingException() : base()
    {
    }
    
    public PageHasUnplannedFormattingException(string message) : base(message)
    {
    }
    
    public PageHasUnplannedFormattingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
