using LatexRendererAPI.Models.Domain;
using LatexRendererAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Data
{
    public class AppDbContext : DbContext
    {
        private IFileService fileService;

        public AppDbContext(DbContextOptions dbContextOptions, IFileService _fileService)
            : base(dbContextOptions)
        {
            fileService = _fileService;
        }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<VersionModel> Versions { get; set; }
        public DbSet<FileModel> Files { get; set; }
        public DbSet<UserProject> UserProjects { get; set; }
        public DbSet<StarProject> StarProjects { get; set; }

        public override int SaveChanges()
        {
            var deletedFileEntities = ChangeTracker
                .Entries<FileModel>()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            foreach (var entity in deletedFileEntities)
            {
                var deletedContent = entity.Entity.Content;
                var duplicateContentRecords = Files
                    .Where(fm => fm.Type == "img" && fm.Content == deletedContent && fm.Id != entity.Entity.Id)
                    .ToList();
                if (!duplicateContentRecords.Any())
                {
                    fileService.DeleteFileRelativePath(deletedContent);
                }
            }

            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<VersionModel>()
                .HasOne(up => up.Project)
                .WithMany(b => b.Versions)
                .HasForeignKey(e => e.ProjectId);

            modelBuilder
                .Entity<ProjectModel>()
                .HasMany(up => up.Versions)
                .WithOne(b => b.Project)
                .HasForeignKey(e => e.ProjectId);

            // modelBuilder
            //     .Entity<UserProject>()
            //     .HasOne(up => up.Editor)
            //     .WithMany(e => e.UserProjects)
            //     .HasForeignKey(up => up.EditorId);

            modelBuilder
                .Entity<UserProject>()
                .HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);
        }
    }
}
