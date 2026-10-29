using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

/// <summary>
///  Website behaves unplanned way, compared to an expected behavior that was to the moment of programming
/// </summary>
public class WebSiteUnplannedBehaviorException : WebPageReadException
{
    public WebSiteUnplannedBehaviorException() : base()
    {
    }
    
    public WebSiteUnplannedBehaviorException(string message) : base(message)
    {
    }
    
    public WebSiteUnplannedBehaviorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
