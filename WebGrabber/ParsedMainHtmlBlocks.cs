namespace VkRadio.WebTools.WebGrabber;

public class ParsedMainHtmlBlocks : ParsedWebPage
{
    public string? Url { get; set; }
    
    public string? RawHtml { get; set; }
    
    public HtmlAgilityPack.HtmlNode? HtmlRoot { get; set; }
    
    public HtmlAgilityPack.HtmlNode? Head { get; set; }
    
    public HtmlAgilityPack.HtmlNode? Body { get; set; }
    
    public string? Title { get; set; }
}
