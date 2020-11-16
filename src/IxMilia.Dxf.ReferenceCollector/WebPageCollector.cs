using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace IxMilia.Dxf.ReferenceCollector
{
    public class WebPageCollector
    {
        private WebClient _client;
        private Uri _virtualRoot;
        private Uri _startPageUrl;
        private string _resultFile;

        public WebPageCollector(string startPageUrl, string virtualRoot, string resultFile)
        {
            _client = new WebClient();
            _startPageUrl = new Uri(startPageUrl);
            _virtualRoot = new Uri(virtualRoot);
            _resultFile = resultFile;
        }

        public void Run()
        {
            var urlsToProcess = new Queue<Uri>();
            urlsToProcess.Enqueue(_startPageUrl);
            var seenUrls = new HashSet<Uri>();
            var styleSheets = new List<Uri>();
            var body = new XElement("body");
            ProcessUrls(urlsToProcess, seenUrls, styleSheets, body);

            // inline styles
            var head = new XElement("head",
                styleSheets.Distinct().Select(s => new XElement("style", _client.DownloadString(s))));

            var html = new XElement("html", head, body);
            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), html);
            Console.WriteLine($"Writing result to {_resultFile}");
            using (var writer = XmlWriter.Create(_resultFile, new XmlWriterSettings() { Indent = true }))
            {
                doc.WriteTo(writer);
            }
        }

        private void ProcessUrls(Queue<Uri> urlsToProcess, HashSet<Uri> seenUrls, List<Uri> styleSheets, XElement body)
        {
            while (urlsToProcess.Count > 0)
            {
                var pageUrl = urlsToProcess.Dequeue();
                seenUrls.Add(pageUrl);
                Console.WriteLine($"Processing {pageUrl}");
                var content = _client.DownloadString(pageUrl); // TODO: handle exceptions
                var xml = XDocument.Parse(content);

                foreach (var element in xml.Descendants())
                {
                    switch (element.Name.LocalName.ToLowerInvariant())
                    {
                        case "a":
                            var href = element.Attribute("href");
                            if (href != null)
                            {
                                var hrefUrl = MakeAbsoluteUrl(pageUrl, href.Value);
                                hrefUrl = new Uri(hrefUrl.AbsoluteUri);

                                // rewrite link to anchor
                                href.Value = "#" + GetAnchorName(hrefUrl);

                                // add url to backlog
                                if (!seenUrls.Contains(hrefUrl) && hrefUrl.OriginalString.StartsWith(_virtualRoot.OriginalString))
                                {
                                    urlsToProcess.Enqueue(hrefUrl);
                                }
                            }
                            break;
                        case "img":
                            // inline image
                            var src = element.Attribute("src");
                            if (src != null)
                            {
                                var imageUrl = MakeAbsoluteUrl(pageUrl, src.Value);
                                Console.WriteLine($"Inlining image {imageUrl}");
                                var imageExtension = Path.GetExtension(imageUrl.PathAndQuery);
                                imageExtension = string.IsNullOrEmpty(imageExtension) ? "png" : imageExtension;
                                if (imageExtension.StartsWith('.'))
                                {
                                    // remove leading dot
                                    imageExtension = imageExtension.Substring(1);
                                }
                                var imageBytes = _client.DownloadData(imageUrl);
                                src.Value = $"data:image/{imageExtension};base64,{Convert.ToBase64String(imageBytes)}";
                            }
                            break;
                        case "link":
                            if (element.Attribute("rel")?.Value == "stylesheet")
                            {
                                var cssUrl = MakeAbsoluteUrl(pageUrl, element.Attribute("href").Value);
                                styleSheets.Add(cssUrl);
                            }
                            break;
                    }
                }

                // emit the body of the page
                var div = new XElement("div", new XAttribute("id", GetAnchorName(pageUrl)), xml.Root.Element("body").Elements());
                body.Add(div);
            }
        }

        private Uri MakeAbsoluteUrl(Uri pageUrl, string relativeOrAbsoluteUrl)
        {
            if (relativeOrAbsoluteUrl.StartsWith("/"))
            {
                return new Uri(pageUrl.Scheme + "://" + pageUrl.Host + relativeOrAbsoluteUrl);
            }
            else if (relativeOrAbsoluteUrl.StartsWith("http://") || relativeOrAbsoluteUrl.StartsWith("https://"))
            {
                return new Uri(relativeOrAbsoluteUrl);
            }
            else
            {
                var lastSlash = pageUrl.OriginalString.LastIndexOf('/');
                var pageDirectoryUrl = pageUrl.OriginalString.Substring(0, lastSlash + 1);
                return new Uri(pageDirectoryUrl + relativeOrAbsoluteUrl);
            }
        }

        private static string GetAnchorName(Uri pageUrl)
        {
            return $"named-anchor-{Path.GetFileNameWithoutExtension(pageUrl.PathAndQuery)}";
        }
    }
}
