using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using budimeb.DAL;
using budimeb.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;

namespace budimeb.ViewComponents
{
    public class CategoryViewComponent : ViewComponent
    {
        PhotoContext db;

        public CategoryViewComponent(PhotoContext db)
        {
            this.db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            ViewBag.Categories = await db.Categories.OrderBy(a => a.Name).ToListAsync();
            return View("_Category");
        }

    }
}