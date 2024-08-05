using budimeb.DAL;
using budimeb.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

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

                    using (var inputStream = photo.OpenReadStream())
                    using (var outputStream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageResizer.ResizeImage(inputStream, outputStream, 800, 800, 80); // Adjust dimensions and quality as needed
                    }

                    newPhoto.PhotoPath = "/uploads/" + fileName;
                    db.Photos.Update(newPhoto);
                    await db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Category(int categoryId, int? page)
        {
            ViewBag.CategoryName = await db.Categories.Where(c => c.Id == categoryId).Select(c => c.Name).FirstOrDefaultAsync();
            ViewBag.Icon = db.Categories.Where(c => c.Id == categoryId).Select(c => c.IconClass).FirstOrDefault();
            ViewBag.CategoryId = categoryId;
            var projects = db.Projects.Where(p => p.CategoryId == categoryId).Include(p => p.Photos).OrderByDescending(p => p.CreatedDate);
            int pageSize = 30;
            int pageNumber = (page ?? 1);
            var pagedProjects = await projects.ToPagedListAsync(pageNumber, pageSize);
            return View(pagedProjects);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var project = await db.Projects
                                  .Include(p => p.Photos)
                                  .FirstOrDefaultAsync(p => p.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, List<IFormFile> photos, string photosToRemove, Dictionary<int, string> PhotoDescriptions)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            bool verify = true;

            // Sprawdź, czy wybrano zdjęcia, jeśli są dodawane nowe zdjęcia
            if (photos != null && photos.Count > 0)
            {
                if (photos.Count > 7)
                {
                    ViewData["PhotosError"] = "Za dużo plików (max 7)";
                    verify = false;
                }
            }
            else if (photos == null)
            {
                // Jeśli nie wybrano nowych zdjęć, nie pokazuj błędu
                ViewData["PhotosError"] = null;
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
                return View(project);
            }

            // Load the original project with related category and photos
            var originalProject = await db.Projects
                                          .Include(p => p.Category)
                                          .Include(p => p.Photos)
                                          .FirstOrDefaultAsync(p => p.ProjectId == id);
            if (originalProject == null)
            {
                return NotFound();
            }

            // Update the project details
            originalProject.Description = project.Description;
            originalProject.CategoryId = project.CategoryId;

            // Check if the category has changed
            var newCategory = await db.Categories.FindAsync(project.CategoryId);
            if (newCategory == null)
            {
                return NotFound("New category not found");
            }

            var normalizedCategoryName = NormalizeFileName(newCategory.Name);
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
            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    if (photo.Length > 0 && (Path.GetExtension(photo.FileName).ToLower() == ".jpg" || Path.GetExtension(photo.FileName).ToLower() == ".jpeg"))
                    {
                        var newPhoto = new Photo
                        {
                            ProjectId = project.ProjectId,
                            UploadedDate = DateTime.Now,
                            Description = PhotoDescriptions.ContainsKey(0) ? PhotoDescriptions[0] : string.Empty // Default description for new photos
                        };

                        db.Photos.Add(newPhoto);
                        await db.SaveChangesAsync();

                        var photoId = newPhoto.PhotoId;
                        var fileName = $"{normalizedCategoryName}_{project.ProjectId}_{photoId}{Path.GetExtension(photo.FileName)}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                        using (var inputStream = photo.OpenReadStream())
                        using (var outputStream = new FileStream(filePath, FileMode.Create))
                        {
                            ImageResizer.ResizeImage(inputStream, outputStream, 800, 800, 80); // Adjust dimensions and quality as needed
                        }
                        newPhoto.PhotoPath = "/uploads/" + fileName;
                        db.Photos.Update(newPhoto);
                        await db.SaveChangesAsync();
                    }
                }
            }

            // Aktualizacja opisów istniejących zdjęć
            foreach (var photoDesc in PhotoDescriptions)
            {
                var photo = originalProject.Photos.FirstOrDefault(p => p.PhotoId == photoDesc.Key);
                if (photo != null)
                {
                    photo.Description = photoDesc.Value;
                    db.Photos.Update(photo);
                }
            }

            await db.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
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

        public IActionResult Privacy()
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
            ViewBag.Name=category.Name;
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
                // Jeśli photo ma być usunięte
                if (deletePhoto && !string.IsNullOrEmpty(existingCategory.PhotoPath))
                {
                    // Usuń istniejące zdjęcie z serwera
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingCategory.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    existingCategory.PhotoPath = null;
                }

                // Sprawdź, czy dodano nowe zdjęcie
                if (photo != null && (Path.GetExtension(photo.FileName).ToLower() == ".jpg" || Path.GetExtension(photo.FileName).ToLower() == ".jpeg"))
                {
                    // Usuń istniejące zdjęcie, jeśli nie jest oznaczone do usunięcia
                    if (!deletePhoto && !string.IsNullOrEmpty(existingCategory.PhotoPath))
                    {
                        var existingFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingCategory.PhotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(existingFilePath))
                        {
                            System.IO.File.Delete(existingFilePath);
                        }
                    }

                    // Normalizuj nazwę pliku
                    var normalizedCategoryName = NormalizeFileName(category.Name);

                    var fileName = $"{normalizedCategoryName}{Path.GetExtension(photo.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    existingCategory.PhotoPath = "/uploads/" + fileName;
                }
                else if (!deletePhoto && !string.IsNullOrEmpty(existingCategory.PhotoPath))
                {
                    // Jeśli nowe zdjęcie nie zostało dodane i deletePhoto jest false, zachowaj istniejące zdjęcie
                    existingCategory.PhotoPath = existingCategory.PhotoPath;
                }

                // Zachowaj IconClass
                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description; // Przykładowo, dostosuj inne właściwości
                                                                     // Skopiuj inne właściwości z modelu, jeśli są potrzebne

                db.Entry(existingCategory).State = EntityState.Modified;
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
