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
    public class MenuViewComponent : ViewComponent
    {
        PhotoContext db;

        public MenuViewComponent(PhotoContext db)
        {
            this.db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await db.Categories.Distinct().ToListAsync();

            ViewData["Categories"] = categories;

            return View("_Menu");
        }

    }
}