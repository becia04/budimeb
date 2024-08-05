using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace budimeb.DAL
{
    public class IdentityContext : IdentityDbContext<ApplicationUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Dostosowanie modelu do używania loginu zamiast e-maila
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(e => e.UserName).IsUnique();
            });
        }
    }
}
