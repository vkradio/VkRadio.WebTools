namespace VkRadio.WebTools.WebGrabber;

public class WebPageReadResultRedirect : WebPageReadResultAbstract
{
    public WebPageReadResultRedirect(string location)
    {
        Location = location;
    }


    /// <summary>
    /// URL of a redirect
    /// </summary>
    public string Location { get; set; }
}
