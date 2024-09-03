using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;
using System.Collections.Generic;

namespace FileUploadServiceAPI1
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
