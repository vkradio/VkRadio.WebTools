using System;

namespace VkRadio.WebTools.WebGrabber.Exceptions;

public class ProgramErrorException : WebGrabberException
{
    public ProgramErrorException() : base()
    {
    }
    
    public ProgramErrorException(string message) : base(message)
    {
    }
    
    public ProgramErrorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
