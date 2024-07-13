using budimeb.Models;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

namespace budimeb.DAL
{
    public class PhotoContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Photo> Photos { get; set; }

        public PhotoContext(DbContextOptions<PhotoContext> options) : base(options)
        {
        }
    }
}
