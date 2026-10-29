using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class LoginRequiredException : WebPageReadException
{
    public LoginRequiredException() : base()
    {
    }

    public LoginRequiredException(string message) : base(message)
    {
    }

    public LoginRequiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
