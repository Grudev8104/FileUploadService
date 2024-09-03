using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace FileUploadServiceAPI
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
