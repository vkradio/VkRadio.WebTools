using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

/// <summary>
///  Website works with significant violation(s) of web standards
/// </summary>
public class WebSiteInvalidBehaviorException : WebPageReadException
{
    public WebSiteInvalidBehaviorException() : base()
    {
    }
    
    public WebSiteInvalidBehaviorException(string message) : base(message)
    {
    }
    
    public WebSiteInvalidBehaviorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
