using Microsoft.EntityFrameworkCore;

namespace SeedProject.Persistence.Model
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }
    }
}