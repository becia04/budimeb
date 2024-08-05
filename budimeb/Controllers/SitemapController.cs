using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml;

public class SitemapController : Controller
{
    public IActionResult Index()
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true };
        using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset");
            writer.WriteAttributeString("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9");

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Index", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("About", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Add", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Category", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Contact", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Edit", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("EditCategory", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Privacy", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Login", "Account", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();
            // Dodaj więcej elementów URL dla innych stron
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Content(sb.ToString(), "application/xml");
    }
}