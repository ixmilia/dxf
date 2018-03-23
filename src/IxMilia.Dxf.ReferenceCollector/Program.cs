using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace IxMilia.Dxf.ReferenceCollector
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required arguments: path to the downloaded files.");
                Console.WriteLine("See README.md in the repository root for instructions on downloading the latest HTML documentation.");
                Environment.Exit(1);
            }

            var rootPath = args[0];
            var initialFile = Path.Combine(rootPath, "GUID-235B22E0-A567-4CF6-92D3-38A2306D73F3.htm");
            if (!File.Exists(initialFile))
            {
                Console.WriteLine($"Unable to find entry file {initialFile}");
                Environment.Exit(1);
            }

            var filesToProcess = new List<string>() { initialFile };
            var seenFiles = new HashSet<string>();
            var remainingFiles = new HashSet<string>(Directory.EnumerateFiles(rootPath, "*.htm", SearchOption.AllDirectories));
            remainingFiles.Remove(initialFile);

            var body = new XElement("body");
            while (filesToProcess.Count > 0)
            {
                var file = filesToProcess[0];
                seenFiles.Add(file);
                filesToProcess.RemoveAt(0);
                var xml = XDocument.Load(file);

                // crawl through the document's links
                foreach (var anchor in xml.Descendants().Where(d => d.Name.LocalName == "a"))
                {
                    var href = anchor.Attribute("href");
                    if (href != null)
                    {
                        var linkPath = Path.Combine(rootPath, href.Value);
                        if (!seenFiles.Contains(linkPath))
                        {
                            filesToProcess.Add(linkPath);
                        }

                        // rewrite to an achor
                        href.Value = $"#named-anchor-{href.Value}";
                    }
                }

                // inline images
                foreach (var img in xml.Descendants().Where(d => d.Name.LocalName == "img"))
                {
                    var src = img.Attribute("src");
                    if (src != null)
                    {
                        var imagePath = Path.Combine(rootPath, src.Value);
                        var imageExtension = Path.GetExtension(imagePath);
                        imageExtension = string.IsNullOrEmpty(imageExtension) ? "png" : imageExtension.Substring(1); // remove leading dot
                        var imageBytes = File.ReadAllBytes(imagePath);
                        Console.WriteLine($"Inlining image from {src.Value}");
                        src.Value = $"data:image/{imageExtension};base64,{Convert.ToBase64String(imageBytes)}";
                    }
                }

                // emit the body of the page
                var div = new XElement("div", new XAttribute("id", $"named-anchor-{Path.GetFileName(file)}"), xml.Root.Element("body").Elements());
                body.Add(div);
            }

            var html = new XElement("html",
                new XElement("head",
                    new XElement("style", new WebClient().DownloadString("http://help.autodesk.com/view/clientframework/client.css"))),
                body);

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), html);
            var resultPath = Path.Combine(rootPath, "DxfReference.html");
            using (var writer = XmlWriter.Create(resultPath, new XmlWriterSettings() { Indent = true }))
            {
                doc.WriteTo(writer);
            }

            Console.WriteLine($"Standalone HTML written to {resultPath}");
        }
    }
}
