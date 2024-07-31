using budimeb.DAL;
using budimeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Add(int categoryId, string name, string desc, List<IFormFile> photos, List<string> descriptions)
        {
            bool verify = true;

            if (photos == null || photos.Count == 0)
            {
                ViewData["PhotosError"] = "Wybierz zdjęcia";
                verify = false;
            }
            if (photos.Count > 7)
            {
                ViewData["PhotosError"] = "Za dużo plików (max 7)";
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

            for (var i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                var description = descriptions != null && descriptions.Count > i ? descriptions[i] : string.Empty;
                if (photo.Length > 0 && (Path.GetExtension(photo.FileName).ToLower() == ".jpg" || Path.GetExtension(photo.FileName).ToLower() == ".jpeg"))
                {
                    var newPhoto = new Photo
                    {
                        ProjectId = project.ProjectId,
                        Description = description,
                        UploadedDate = DateTime.Now
                    };

                    db.Photos.Add(newPhoto);
                    await db.SaveChangesAsync();

                    var photoId = newPhoto.PhotoId;
                    var normalizedCategoryName = NormalizeFileName(category.Name);
                    var fileName = $"{normalizedCategoryName}_{project.ProjectId}_{photoId}{Path.GetExtension(photo.FileName)}";
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

        public async Task<IActionResult> Category(int categoryId)
        {
            var category = await db.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound();
            }

            var projects = db.Projects
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Photos)
                .ToList();

            ViewBag.CategoryName = category.Name;
            return View(projects);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var project = await db.Projects.Include(p => p.Photos).FirstOrDefaultAsync(p => p.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, List<IFormFile> photos, string photosToRemove)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            bool verify = true;

            if (photos == null || photos.Count == 0)
            {
                ViewData["PhotosError"] = "Wybierz zdjęcia";
                verify = false;
            }
            if (photos.Count > 7)
            {
                ViewData["PhotosError"] = "Za dużo plików (max 7)";
                verify = false;
            }

            var category = await db.Categories.FindAsync(project.CategoryId);
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
            // Load the original project with related category and photos
            var originalProject = await db.Projects.Include(p => p.Category).Include(p => p.Photos).FirstOrDefaultAsync(p => p.ProjectId == id);
                    if (originalProject == null)
                    {
                        return NotFound();
                    }

                    // Update the project details
                    originalProject.Name = project.Name;
                    originalProject.Description = project.Description;
                    originalProject.CategoryId = project.CategoryId;

                    var newCategory = await db.Categories.FindAsync(project.CategoryId);
                    if (newCategory == null)
                    {
                        return NotFound("New category not found");
                    }

                    var normalizedCategoryName = NormalizeFileName(newCategory.Name);

                    // Check if the category has changed
                    if (originalProject.CategoryId != project.CategoryId)
                    {
                        // Update existing photo file names
                        foreach (var photo in originalProject.Photos)
                        {
                            var oldFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + photo.PhotoPath);
                            var newFileName = $"{normalizedCategoryName}_{photo.ProjectId}_{photo.PhotoId}{Path.GetExtension(oldFileName)}";
                            var newFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", newFileName);

                            if (System.IO.File.Exists(oldFileName))
                            {
                                System.IO.File.Move(oldFileName, newFilePath);
                            }

                            photo.PhotoPath = "/uploads/" + newFileName;
                            db.Photos.Update(photo);
                        }

                        await db.SaveChangesAsync();
                    }

                    // Usuwanie zdjęć
                    if (!string.IsNullOrEmpty(photosToRemove))
                    {
                        var photoIdsToRemove = photosToRemove.Split(',').Select(int.Parse).ToList();
                        var photosToDelete = db.Photos.Where(p => photoIdsToRemove.Contains(p.PhotoId)).ToList();

                        foreach (var photo in photosToDelete)
                        {
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + photo.PhotoPath);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }

                            db.Photos.Remove(photo);
                        }

                        await db.SaveChangesAsync();
                    }

                    // Dodawanie nowych zdjęć
                    foreach (var photo in photos)
                    {
                        if (photo.Length > 0 && (Path.GetExtension(photo.FileName).ToLower() == ".jpg" || Path.GetExtension(photo.FileName).ToLower() == ".jpeg"))
                        {
                            var newPhoto = new Photo
                            {
                                ProjectId = project.ProjectId,
                                UploadedDate = DateTime.Now
                            };

                            db.Photos.Add(newPhoto);
                            await db.SaveChangesAsync();

                            var photoId = newPhoto.PhotoId;
                            var fileName = $"{normalizedCategoryName}_{project.ProjectId}_{photoId}{Path.GetExtension(photo.FileName)}";
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

                    await db.SaveChangesAsync();

            ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(project);
        }
        private string NormalizeFileName(string fileName)
        {
            var normalizedString = fileName
                .ToLowerInvariant()
                .Replace('ą', 'a')
                .Replace('ć', 'c')
                .Replace('ę', 'e')
                .Replace('ł', 'l')
                .Replace('ń', 'n')
                .Replace('ó', 'o')
                .Replace('ś', 's')
                .Replace('ź', 'z')
                .Replace('ż', 'z');

            // Remove any invalid characters
            normalizedString = string.Concat(normalizedString.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));

            return normalizedString;
        }

        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await db.Projects.Include(p => p.Photos).FirstOrDefaultAsync(p => p.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            try
            {
                // Usuwanie zdjęć z systemu plików
                foreach (var photo in project.Photos)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + photo.PhotoPath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Usuwanie zdjęć z bazy danych
                db.Photos.RemoveRange(project.Photos);

                // Usuwanie projektu z bazy danych
                db.Projects.Remove(project);
                await db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Obsługa błędów, np. logowanie
                ViewData["Error"] = "Wystąpił błąd podczas usuwania projektu: " + ex.Message;
                return View("Edit", project); // W przypadku błędu, powróć do widoku edycji z wyświetleniem błędu
            }
        }


        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewBag.YearsOfExperience = DateTime.Now.Year - 2003;

            return View();
        }

        public IActionResult Index()
        {
            ViewBag.Categories = db.Categories.OrderBy(a => a.Name).ToList();
            return View();
        }

        public IActionResult EditCategory(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category category, IFormFile photo, bool deletePhoto)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            var existingCategory = await db.Categories.FindAsync(id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (deletePhoto && !string.IsNullOrEmpty(existingCategory.PhotoPath))
                {
                    // Delete existing photo from server
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingCategory.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    existingCategory.PhotoPath = null;
                }

                if (photo != null && (Path.GetExtension(photo.FileName).ToLower() == ".jpg" || Path.GetExtension(photo.FileName).ToLower() == ".jpeg"))
                {
                    // Delete existing photo from server if necessary
                    if (!deletePhoto && !string.IsNullOrEmpty(existingCategory.PhotoPath))
                    {
                        var existingFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingCategory.PhotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(existingFilePath))
                        {
                            System.IO.File.Delete(existingFilePath);
                        }
                    }

                    // Normalize category name
                    var normalizedCategoryName = NormalizeFileName(category.Name);

                    var fileName = $"{normalizedCategoryName}{Path.GetExtension(photo.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    category.PhotoPath = "/uploads/" + fileName;
                }
                else if (!deletePhoto && !string.IsNullOrEmpty(existingCategory.PhotoPath))
                {
                    // If no new photo is uploaded and deletePhoto is false, keep existing photo
                    category.PhotoPath = existingCategory.PhotoPath;
                }

                db.Entry(existingCategory).CurrentValues.SetValues(category);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

       
    }
}
