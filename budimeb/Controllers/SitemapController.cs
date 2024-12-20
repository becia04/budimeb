using budimeb.Controllers;
using budimeb.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Xml;

public class SitemapController : Controller
{
    private readonly PhotoContext db;

    public SitemapController(PhotoContext db)
    {
        this.db = db;
    }
    public IActionResult Index()
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true };

        using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset");
            

            // Dodaj statyczne podstrony
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Index", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("About", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Contact", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();


            writer.WriteStartElement("url");
            writer.WriteElementString("loc", Url.Action("Privacy", "Home", null, Request.Scheme));
            writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
            writer.WriteEndElement();

        // Dynamiczne adresy URL dla projektów
        var categories = db.Categories.ToList(); // Pobranie projektów z bazy danych
            foreach (var category in categories)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", Url.Action("Category", "Home", new { id = category.Id }, Request.Scheme));
                writer.WriteElementString("lastmod", System.DateTime.UtcNow.ToString("yyyy-MM-dd"));
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // zamknij urlset
            writer.WriteEndDocument();
        }

        return Content(sb.ToString(), "application/xml");
    }
}