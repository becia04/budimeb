using budimeb.DAL;
using budimeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace budimeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PhotoContext db;

        public HomeController(PhotoContext db, ILogger<HomeController> logger)
        {
            this.db = db;
            _logger = logger;
        }

        public IActionResult Add()
        {
            ViewBag.Categories = db.Categories.OrderBy(a => a.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int categoryId, string name, string desc, List<IFormFile> photos)
        {
            bool verify = true;

            if (photos == null || photos.Count == 0)
            {
                ViewData["PhotosError"] = "Wybierz zdjęcia";
                verify = false;
            }
            if (photos.Count > 8)
            {
                ViewData["PhotosError"] = "Za dużo plików (max 8)";
                verify = false;
            }

            var category = await db.Categories.FindAsync(categoryId);
            if (category == null)
            {
                ViewData["CategoryError"] = "Wybierz kategorię";
                verify = false;
            }

            if (!verify)
            {
                ViewBag.Categories = db.Categories.OrderBy(a => a.Name).ToList();
                return View();
            }

            var project = new Project
            {
                Name = name,
                Description = desc,
                CategoryId = categoryId,
                CreatedDate = DateTime.Now
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var projectId = project.ProjectId;

            foreach (var photo in photos)
            {
                if (photo.Length > 0 && (Path.GetExtension(photo.FileName).ToLower() == ".jpg" || Path.GetExtension(photo.FileName).ToLower() == ".jpeg"))
                {
                    var newPhoto = new Photo
                    {
                        ProjectId = projectId,
                        UploadedDate = DateTime.Now
                    };

                    db.Photos.Add(newPhoto);
                    await db.SaveChangesAsync();

                    var photoId = newPhoto.PhotoId;
                    var fileName = $"{category.Name}_{projectId}_{photoId}{Path.GetExtension(photo.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    newPhoto.PhotoPath = "/uploads/" + fileName;
                    db.Photos.Update(newPhoto);
                    await db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index", "Home");
        }

    public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
