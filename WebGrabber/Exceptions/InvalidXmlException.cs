using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class InvalidXmlException : WebPageParseException
{
    public InvalidXmlException() : base()
    {
    }

    public InvalidXmlException(string message) : base(message)
    {
    }

    public InvalidXmlException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
