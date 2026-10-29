using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using VkRadio.WebTools.WebGrabber.Exceptions;
using VkRadio.WebTools.WebGrabber.Utils;
using Ex = VkRadio.WebTools.WebGrabber.Exceptions;

// NOTES: Use Fiddler to debug HTTP headers

namespace VkRadio.WebTools.WebGrabber;

/// <summary>
///  Base grabber of webpages - it supports reading HTML as a string, and base parse of it
/// </summary>
public class WebGrabber
{
    readonly object _lockLoginStateVars = new object();
    readonly object _lockDupesCache = new object();

    #region Additional processing for cookies
    /// <summary>
    ///  Days of week
    /// <para>Some websites, like those hosted on forum2x2, return a cookie with invalid formatting -
    /// it has key-value pairs not only separated by commas - also date values itself contain commas,
    /// so the standard cookie parsed gots crashed. We use those constant values to repair such cookie parser.
    /// </para>
    /// </summary>
    static readonly string[] c_weekdaysForFixingInvalidCookies = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

    /// <summary>
    ///  Is cookie a part of broken date value
    /// </summary>
    /// <param name="cookieValue"></param>
    /// <returns></returns>
    protected bool IsCookieAPartOfDate(string cookieValue)
    {
        var isDate = false;

        if (cookieValue != null && cookieValue.Length > 3)
        {
            var tail = cookieValue.Substring(cookieValue.Length - 3, 3);

            foreach (var weekDay in c_weekdaysForFixingInvalidCookies)
            {
                if (tail == weekDay)
                {
                    isDate = true;
                    break;
                }
            }
        }

        return isDate;
    }

    /// <summary>
    ///  Fix the cookie collection
    /// </summary>
    /// <param name="httpRequest">HTTP request</param>
    /// <param name="httpResponse">HTTP response that contains cookies</param>
    protected void FixCookieCollection(HttpWebRequest httpRequest, HttpWebResponse httpResponse)
    {
        for (var i = 0; i < httpResponse.Headers.Count; i++)
        {
            var name = httpResponse.Headers.GetKey(i);

            if (name != "Set-Cookie")
            {
                continue;
            }

            var value = httpResponse.Headers.Get(i);
            var cookies = new List<string>();

            foreach (var singleCookie in value.Split(','))
            {
                if (cookies.Count != 0 && IsCookieAPartOfDate(cookies[cookies.Count - 1]))
                {
                    cookies[cookies.Count - 1] = cookies[cookies.Count - 1] + singleCookie.Trim();
                }
                else
                {
                    cookies.Add(singleCookie.Trim());
                }
            }

            foreach (var singleCookie in cookies)
            {
                var match = Regex.Match(singleCookie, "(.+?)=(.+?);");

                if (match.Captures.Count == 0)
                {
                    continue;
                }

                httpResponse.Cookies.Add(
                    new Cookie(
                        match.Groups[1].ToString(),
                        match.Groups[2].ToString(),
                        "/",
                        httpRequest.Host.Split(':')[0]
                    )
                );
            }
        }
    }

    /// <summary>
    ///  Create a cookie line from a collection of cookies for reuse
    /// </summary>
    /// <param name="cookieCollection"></param>
    /// <returns></returns>
    protected string CreateCookieString(CookieCollection cookieCollection)
    {
        var result = string.Empty;

        foreach (Cookie cookie in cookieCollection)
        {
            if (cookie.Value == "deleted")
            {
                continue;
            }

            if (result != string.Empty)
            {
                result += "; ";
            }

            result += cookie.Name + "=" + cookie.Value;
        }

        return result;
    }
    #endregion

    void DoLogin()
    {
        lock(_lockLoginStateVars)
        {
            if (!_loginCredentialsAreSet)
            {
                throw new Ex.InvalidUsageException(Messages.LoginCredentialsNotSet);
            }

            if (_loginState == LoginStateEnum.LoggedIn)
            {
                throw new Ex.InvalidUsageException(Messages.AlreadyLoggedIn);
            }

            var httpRequest = CreateLoginHttpRequest();

            try
            {
                using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    // Redirect means successful login, because for an oridinal web browser it will be redirected
                    // to a page that contains the required forum topic (or any other page that requested a password-protected
                    // log-in)
                    if (httpResponse.StatusCode == HttpStatusCode.Redirect)
                    {
                        if (httpResponse.Cookies.Count == 0)
                        {
                            FixCookieCollection(httpRequest, httpResponse);
                        }

                        Cookie = CreateCookieString(httpResponse.Cookies);
                        _loginState = LoginStateEnum.LoggedIn;
                    }
                    else
                    {
                        throw new Ex.WebSiteInvalidBehaviorException(string.Format(Messages.WebSiteNotRedirectsAfterLogin, httpResponse.StatusCode));
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Ex.TimeoutException();
                }
                else
                {
                    throw new Ex.WebSiteUnplannedBehaviorException(string.Format(Messages.WebSiteExceptsAfterLogin, ex.ToString()), ex);
                }
            }
            catch (Ex.WebGrabberException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Ex.WebSiteUnplannedBehaviorException(string.Format(Messages.WebSiteExceptsAfterLogin, ex.ToString()), ex);
            }
        }
    }

    protected HttpWebRequest CreateHttpRequest(string url, HttpRequestTypeEnum requestType = HttpRequestTypeEnum.Get, string? queryString = null)
    {
        var httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);

        httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:43.0) Gecko/20100101 Firefox/43.0";
        httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        httpRequest.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
        httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        httpRequest.Headers.Add("X-Compress", "1");
        httpRequest.AllowAutoRedirect = false;

        if (requestType == HttpRequestTypeEnum.Post)
        {
            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/x-www-form-urlencoded";

            if (!string.IsNullOrEmpty(queryString))
            {
                var queryBytes = Encoding.ASCII.GetBytes(queryString);
                httpRequest.ContentLength = queryBytes.Length;

                using (Stream requestStream = httpRequest.GetRequestStream())
                {
                    requestStream.Write(queryBytes, 0, queryBytes.Length);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(Cookie))
        {
            httpRequest.Headers.Add(HttpRequestHeader.Cookie, Cookie);
        }

        return httpRequest;
    }

    /// <summary>
    ///  Create an HttpWebRequest object to be used to log in
    /// </summary>
    /// <returns></returns>
    protected virtual HttpWebRequest CreateLoginHttpRequest()
    {
        throw new Ex.InvalidUsageException(string.Format(Messages.CreateLoginHttpRequestIsAbstract, GetType().Name, MethodBase.GetCurrentMethod().Name));
    }

    /// <summary>
    ///  Detect whether Location field in HTTP response points to a log-in page
    /// </summary>
    /// <param name="locationInHeader">Location content of an HTTP response header</param>
    /// <returns></returns>
    protected virtual bool IsHttpResponseHeaderLocationRedirectsToLoginPage(string? locationInHeader) => false;

    /// <summary>
    ///  Overridable parser of webpages for a concrete parser implementation for each concrete website
    /// </summary>
    /// <param name="url">Webpage URL</param>
    /// <param name="parseSpecification">Parse specification</param>
    /// <param name="parsedProto">Pre-parsed base elements</param>
    /// <returns>Parsed web page</returns>
    protected virtual ParsedWebPage ParseConcrete(string url, ParseSpecification parseSpecification, ParsedMainHtmlBlocks parsedProto) => parsedProto;

    protected virtual void CustomPreLogin()
    {
    }

    #region Clean-up an HTMP soup, convert it to XHTML suitable to work with XML parser
    bool IsCharacter(int character)
    {
        return character < 0xFDD0 || // everything below here is fine
            character > 0xFDEF &&    // exclude the 0xFFD0...0xFDEF non-characters
            (character & 0xfffE) != 0xFFFE; // exclude all other non-characters
    }

    string ReplaceNonCharacters(string stringValue, char replacement)
    {
        var builder = new StringBuilder(stringValue.Length);

        for (var i = 0; i < stringValue.Length; i++)
        {
            if (char.IsSurrogatePair(stringValue, i))
            {
                int c = char.ConvertToUtf32(stringValue, i);
                i++;

                if (IsCharacter(c))
                {
                    builder.Append(char.ConvertFromUtf32(c));
                }
                else
                {
                    builder.Append(replacement);
                }
            }
            else
            {
                char c = stringValue[i];

                if (IsCharacter(c))
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append(replacement);
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///  Remove HTML comments
    /// </summary>
    /// <param name="htmlString">HTML string</param>
    /// <returns>HTML string with removed comments</returns>
    string CutHtmlComments(string htmlString)
    {
        string result = htmlString;

        var indexOf = result.IndexOf("<!--", 0);

        while (indexOf != -1)
        {
            var indexOfCloseComment = result.IndexOf("-->", indexOf + 1);

            if (indexOfCloseComment == -1)
            {
                throw new CannotNormalizeHtmlSoupException("Closing comment index not found");
            }

            result = result.Substring(0, indexOf) + result.Substring(indexOfCloseComment + 3);

            indexOf = result.IndexOf("<!--", 0);
        }

        return result;
    }

    /// <summary>
    ///  Remove &lt;script&gt; tags with their contents
    /// </summary>
    /// <param name="htmlValue"></param>
    /// <returns></returns>
    string CutJavaScripts(string htmlValue)
    {
        const string c_scriptOpenTag = "<script";
        const string c_scriptClosingTag = "</script>";

        var lenScriptOpenTag = c_scriptOpenTag.Length;
        var lenScriptClosingTag = c_scriptClosingTag.Length;

        var result = htmlValue;

        var indexOf = 0;
        var numberOfScriptTag = 0;

        indexOf = result.IndexOf(c_scriptOpenTag, indexOf, StringComparison.InvariantCultureIgnoreCase);

        while (indexOf >= 0)
        {
            numberOfScriptTag++;

            var indexOfClosing = result.IndexOf(c_scriptClosingTag, indexOf + lenScriptOpenTag, StringComparison.InvariantCultureIgnoreCase);

            if (indexOf < 0)
            {
                throw new CannotNormalizeHtmlSoupException($"When cutting off <script> tags, unable to find a clising </script> tag for an opening tag located at index {indexOf} (<script> tag number {numberOfScriptTag})");
            }

            var beginning = indexOf != 0
                ? result.Substring(0, indexOf)
                : string.Empty;

            var ending = result.Length - (indexOfClosing + lenScriptClosingTag) != 0
                ? result.Substring(indexOfClosing + lenScriptClosingTag)
                : string.Empty;

            result = beginning + ending;

            indexOf = result.IndexOf(c_scriptOpenTag, indexOf, StringComparison.InvariantCultureIgnoreCase);
        }

        return result;
    }

    /// <summary>
    ///  Preliminary cleanup of HTML soup for a concrete website
    /// </summary>
    /// <param name="htmlSoup"></param>
    /// <param name="originalHtmlSoup">HTML souop received from a web server (used for debug purposes)</param>
    /// <returns>Preliminary processed HTML soup before putting it to Tidy</returns>
    protected virtual string CleanHtmlSoupBeforeTidy(string htmlSoup, string originalHtmlSoup) => htmlSoup;

    /// <summary>
    ///  Additional cleanup after Tidy, with following convertion to XHTML
    /// </summary>
    /// <param name="xhtml">XHTML from Tidy</param>
    /// <returns>Final XHTML suitable to putting it to XML parser</returns>
    protected virtual string CleanXhtmlAfterTidy(string xhtml)
    {
        var result = xhtml
            .Replace("\0", " ")
            .Replace("\u001B", " ")
            .Replace("<html xmlns=\"http://www.w3.org/1999/xhtml\"", "<html")
            .Replace(" && ", " ");

        result = ReplaceNonCharacters(result, ' ');
        result = CutHtmlComments(result);
        result = CutJavaScripts(result);
        result = result.Substring(0, result.IndexOf("</html>")) + "</html>";

        return result;
    }

    protected virtual bool CustomAttributeCleanup(ref string inout_xhtml, string exceptionMessage) => false;
    #endregion

    #region Стандартные сообщения для повторного использования.
    protected string MsgLoginCredentialsNotSet => Messages.LoginCredentialsNotSet;
    #endregion

    public LoginStateEnum _loginState = LoginStateEnum.NotLoggedIn;

    /// <summary>
    /// User login string
    /// </summary>
    protected string? _login;

    /// <summary>
    /// User login password
    /// </summary>
    protected string? _password;

    protected bool _loginCredentialsAreSet;
    
    protected Encoding _pageEncoding = Encoding.UTF8;

    public string? Cookie { get; set; }

    /// <summary>
    ///  Constructor for websites without password-protected login
    /// </summary>
    public WebGrabber()
    {
    }

    /// <summary>
    ///  Constructor to be used on websites that require password-protected login
    /// </summary>
    /// <param name="login"></param>
    /// <param name="password"></param>
    public WebGrabber(string login, string password)
    {
        _login = login;
        _password = password;
        _loginCredentialsAreSet = true;
    }

    /// <summary>
    ///  Read HTML of webpage
    /// </summary>
    /// <param name="urlOfWebpage"></param>
    /// <returns>HTML of a page</returns>
    public WebPageReadResultAbstract ReadWebPageAsHtml(string urlOfWebpage)
    {
        // Do 2 tries to read a page:
        // 1. First try to read it without using log-in. If success, that that's the last step.
        // 2. If on the first try we received a request to login, then POST a log-in form, after that repeat a try.

        var loginRequired = false;

        for (var step = 1; step <= 2; step++)
        {
            if (loginRequired)
            {
                DoLogin();
            }

            var httpRequest = CreateHttpRequest(urlOfWebpage);

            try
            {
                using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                {
                    switch (httpResponse.StatusCode)
                    {
                        case HttpStatusCode.OK:

                            using (var reader = new StreamReader(httpResponse.GetResponseStream(), _pageEncoding))
                            {
                                return new WebPageReadResultHtml()
                                {
                                    Content = reader.ReadToEnd() ?? string.Empty
                                };
                            }

                        case HttpStatusCode.Redirect:

                            if (IsHttpResponseHeaderLocationRedirectsToLoginPage(httpResponse.Headers[HttpResponseHeader.Location]))
                            {
                                lock(_lockLoginStateVars)
                                {
                                    if (_loginState == LoginStateEnum.LoggedIn)
                                    {
                                        throw new Ex.WebSiteUnplannedBehaviorException(string.Format(Messages.UnplannedRedirectWhileLoggedIn, urlOfWebpage));
                                    }
                                    else
                                    {
                                        loginRequired = true;
                                    }
                                }
                            }
                            else
                            {
                                return new WebPageReadResultRedirect(httpResponse.Headers[HttpResponseHeader.Location]);
                            }

                            break;

                        default:

                            throw new Ex.WebSiteUnplannedBehaviorException(string.Format(Messages.GetReturnedUnplannedStatusCode, urlOfWebpage, httpResponse.StatusCode));
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Ex.TimeoutException();
                }
                else if (ex.Status == WebExceptionStatus.ConnectFailure
                  || ex.Status == WebExceptionStatus.ConnectionClosed
                  || ex.Status == WebExceptionStatus.NameResolutionFailure
                  || ex.Status == WebExceptionStatus.RequestCanceled
                  || ex.Status == WebExceptionStatus.SendFailure
                  || ex.Status == WebExceptionStatus.UnknownError
                  || (ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.ServiceUnavailable
                  || (ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.InternalServerError)
                {
                    throw new Ex.WebSiteOrNetworkIsDown(ex.Status);
                }
                else
                {
                    throw new Ex.WebSiteUnplannedBehaviorException(string.Format(Messages.WebSiteExceptsDuringGet, urlOfWebpage, ex.ToString()), ex);
                }
            }
            catch (Ex.WebGrabberException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Ex.WebSiteUnplannedBehaviorException(string.Format(Messages.WebSiteExceptsDuringGet, urlOfWebpage, ex.ToString()), ex);
            }
        }

        // We should not get here
        throw new Ex.GeneralException(Messages.ProgramErrorGetPageFunctionEnd);
    }

    void DumpAndThrow(string preparedXhtmlString, string htmlSoup, Exception exception)
    {
        var pathForDump = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        pathForDump = Path.Combine(pathForDump, Guid.NewGuid().ToString() + ".xml");

        using (TextWriter tw = File.CreateText(pathForDump))
        {
            tw.Write(preparedXhtmlString);
        }

        pathForDump += ".txt";

        using (TextWriter tw = File.CreateText(pathForDump))
        {
            tw.Write(exception.Message);
        }

        throw new Ex.InvalidXmlException(Messages.UnableToParseXml, exception)
        {
            RawHtmlSoup = htmlSoup,
            PreparedHtmlAtThisStage = preparedXhtmlString
        };
    }

    public ParsedWebPage ReadAndParseWebPage(string urlOfPage, ParseSpecification parseSpecification)
    {
        lock (_lockLoginStateVars)
        {
            if (_loginState != LoginStateEnum.LoggedIn)
            {
                CustomPreLogin();
            }
        }

        #region Read HTML, and do a redirect if required
        var readRawResult = ReadWebPageAsHtml(urlOfPage);
        var redirectResult = readRawResult as WebPageReadResultRedirect;
        var watchdogCounter = 0;

        while (redirectResult != null)
        {
            // If did redirect 3 times, throw an exception, because it is something oviosly wrong happening
            watchdogCounter++;

            if (watchdogCounter > 3)
            {
                throw new Ex.WebSiteInvalidBehaviorException(string.Format(Messages.TooMuchRedirects, urlOfPage, 3, redirectResult.Location ?? string.Empty));
            }

            readRawResult = ReadWebPageAsHtml(redirectResult.Location);
            redirectResult = readRawResult as WebPageReadResultRedirect;
        }

        var resultHtml = readRawResult as WebPageReadResultHtml;

        if (resultHtml == null)
        {
            throw new Ex.GeneralException(string.Format(Messages.ProgramErrorNeitherHtmlNorRedirect, urlOfPage));
        }
        #endregion

        /*
        // Short introduction to HtmlUnit: http://htmlunit.sourceforge.net/gettingStarted.html
        string htmlSoup = resultHtml.Content;
        // Anyway cleanup lots of garbage
        htmlSoup = CutHtmlComments(htmlSoup);
        htmlSoup = CutJavaScripts(htmlSoup);
        htmlSoup = htmlSoup
            .Replace("<![CDATA[", string.Empty)
            .Replace("]]>", string.Empty);
        // end of garbage
        StringWebResponse response = new StringWebResponse(htmlSoup, new java.net.URL(in_url));
        com.gargoylesoftware.htmlunit.WebClient client = new com.gargoylesoftware.htmlunit.WebClient();
        HtmlPage page = HTMLParser.parseHtml(response, client.getCurrentWindow());
        string preparedXhtmlString = page.asXml();
        */

        #region Cleanup of HTML soup, convert it to a normal XHTML using Tidy and other tools
        string htmlSoup = resultHtml.Content ?? string.Empty;
        htmlSoup = CleanHtmlSoupBeforeTidy(htmlSoup, htmlSoup);
        //htmlSoup = HtmlAgilityPack.HtmlEntity.DeEntitize(htmlSoup);
        //Tidy tidy = new Tidy();
        //tidy.Options.Xhtml = true;
        //tidy.Options.CharEncoding = CharEncoding.UTF8;
        //tidy.Options.IndentAttributes = false;
        //tidy.Options.IndentContent = false;
        //tidy.Options.SmartIndent = false;
        //tidy.Options.LiteralAttribs = false;
        //tidy.Options.QuoteAmpersand = true;
        //tidy.Options.QuoteMarks = true;
        //tidy.Options.QuoteNbsp = true;
        //tidy.Options.XmlOut = true;
        //byte[] buf = Encoding.UTF8.GetBytes(htmlSoup); // Here use UTF8 for "universal" page encoding no matter in which encoding we received a page from a server

        //string preparedXhtmlString;

        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(resultHtml.Content);
        //htmlDoc.OptionOutputAsXml = true;

        //using (var sw = new StringWriter())
        //using (var xw = new XmlTextWriter(sw))
        //{
        //    htmlDoc.Save(xw);
        //    preparedXhtmlString = sw.ToString();
        //}

        //using (var textReader = new StringReader(resultHtml.Content))
        //using (var sgmlReader = new Sgml.SgmlReader())
        //{
        //    sgmlReader.DocType = "HTML";
        //    sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
        //    sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
        //    sgmlReader.InputStream = textReader;

        //    var doc = new XmlDocument();
        //    doc.PreserveWhitespace = true;
        //    doc.XmlResolver = null;
        //    doc.Load(sgmlReader);
        //    preparedXhtmlString = doc.OuterXml;
        //}

        //using (MemoryStream bufIn = new MemoryStream(buf))
        //{
        //    using (MemoryStream bufOut = new MemoryStream())
        //    {
        //        TidyMessageCollection tidyMessagesStub = new TidyMessageCollection();
        //        tidy.Parse(bufIn, bufOut, tidyMessagesStub);
        //        if (tidyMessagesStub.Errors != 0)
        //        {
        //            string errCompound = string.Empty;
        //            foreach (var e in tidyMessagesStub)
        //            {
        //                if (e.ToString().Contains("Error:"))
        //                {
        //                    if (errCompound != string.Empty)
        //                        errCompound += "; ";
        //                    errCompound += e.ToString();
        //                }
        //            }
        //            throw new Ex.CannotNormalizeHtmlSoupException("Tidy не может обработать страницу, выданные ошибки: " + errCompound);
        //        }
        //        preparedXhtmlString = Encoding.UTF8.GetString(bufOut.GetBuffer());
        //    }
        //}


        //preparedXhtmlString = CleanXhtmlAfterTidy(preparedXhtmlString);
        #endregion

        var parsedProto = new ParsedMainHtmlBlocks()
        {
            Url = urlOfPage,
            RawHtml = htmlDoc.DocumentNode.ToString()
        };  //preparedXhtmlString };

        #region Preliminary parse of base HTML blocks
        if (parseSpecification.PreParseMainHtmlBlocks || parseSpecification.ParseTitle)
        {
            /*XElement xelRoot = null;

            bool retry;
            do
            {
                retry = false;
                try
                {
                    xelRoot = XElement.Parse(preparedXhtmlString);
                }
                catch (XmlException ex)
                {
                    Regex regex = new Regex("\\\"" + @"(\S+)" + "\\\"" + @" является повторяющимся именем атрибута\.");
                    Match match = regex.Match(ex.Message);
                    if (match.Success)
                    {
                        if (!CustomAttributeCleanup(ref preparedXhtmlString, ex.Message))
                        {
                            lock(_lockDupesCache)
                                XmlAttributeDupesHelper.AddDupe(match.Groups[1].Value, PathToPostTidificationFile, ref _attributeDuplicates);
                            preparedXhtmlString = CleanXhtmlAfterTidy(preparedXhtmlString);
                        }
                        retry = true;
                    }
                    else
                    {
                        regex = new Regex("Префикс \\\"" + @"(\S+)" + "\\\" необъявленный" + @"\.\, строка (\d+)\,");
                        match = regex.Match(ex.Message);
                        if (match.Success)
                        {
                            string[] lines = preparedXhtmlString.Split(new char[] { '\n' });
                            string suspectLine = lines[int.Parse(match.Groups[2].Value) - 1];
                            string prefix = match.Groups[1].Value;
                            regex = new Regex(prefix + @"\:(\S+)\=" + "\\\"" + prefix + @"\:(\S+)" + "\\\"");
                            match = regex.Match(suspectLine);
                            if (!match.Success)
                                DumpAndThrow(preparedXhtmlString, htmlSoup, ex);
                            lock(_lockDupesCache)
                                XmlAttributeDupesHelper.AddDupe(prefix + ":" + match.Groups[1].Value, PathToPostTidificationFile, ref _attributeDuplicates);
                            preparedXhtmlString = CleanXhtmlAfterTidy(preparedXhtmlString);
                            retry = true;
                        }
                        else
                        {
                            DumpAndThrow(preparedXhtmlString, htmlSoup, ex);
                        }
                    }
                }
            }
            while (retry);*/

            //if (xelRoot.Name.LocalName.ToLower() == "html")
            //if (htmlBody != null)
            {
                //parsedProto.HtmlRoot = xelRoot;
                parsedProto.HtmlRoot = htmlDoc.DocumentNode;

                // Extract HEAD
                var htmlHead = htmlDoc.DocumentNode.SelectSingleNode("//head");
                //XElement xelHead = xelRoot.Element("head");
                //if (xelHead != null)
                if (htmlHead != null)
                {
                    //parsedProto.Head = xelHead;
                    parsedProto.Head = htmlHead;

                    // Extract TITLE if required
                    if (parseSpecification.ParseTitle)
                    {
                        //XElement xelTitle = xelHead.Element("title");
                        var htmlTitle = htmlHead.SelectSingleNode("//title");
                        //if (xelTitle != null)
                        if (htmlTitle != null)
                        {
                            //parsedProto.Title = xelTitle.Value ?? string.Empty;
                            parsedProto.Title = (htmlTitle.InnerText ?? string.Empty).ReconstructSingleLineAndStripHtmlEntities();
                        }
                    }
                }

                // Extract BODY
                //XElement xelBody = xelRoot.Element("body");
                var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");
                //if (xelBody != null)
                if (htmlBody != null)
                {
                    //parsedProto.Body = xelBody;
                    parsedProto.Body = htmlBody;
                }
            }
            //else if (xelRoot.Name.LocalName.ToLower() == "body") // It is possible that root element will be BODY
            //{
            //    parsedProto.Body = xelRoot;
            //}
        }
        #endregion

        // Do a concrete implementation of parse for a concrete website, if defined by overriden method.
        // Otherwise just return a preliminary parsed result with base elements.
        return ParseConcrete(urlOfPage, parseSpecification, parsedProto);
    }
}
