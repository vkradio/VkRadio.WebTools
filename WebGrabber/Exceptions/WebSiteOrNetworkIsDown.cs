using System;
using System.Net;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class WebSiteOrNetworkIsDown : WebPageReadException
{
    public WebExceptionStatus Status { get; private set; }

    public WebSiteOrNetworkIsDown() : base()
    {
    }
    
    public WebSiteOrNetworkIsDown(string message) : base(message)
    {
    }
    
    public WebSiteOrNetworkIsDown(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    public WebSiteOrNetworkIsDown(WebExceptionStatus status) : base()
    {
        Status = status;
    }
}
