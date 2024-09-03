using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace ProcessedFilesServiceAPI
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProcessedFile> ProcessedFiles { get; set; }
    }
}
