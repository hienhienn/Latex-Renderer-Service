using LatexRendererAPI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions) { }

        public DbSet<UserModel> Users { get; set; }

        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<VersionModel> Versions { get; set; }
        public DbSet<FileModel> Files { get; set; }
        public DbSet<UserProject> UserProjects { get; set; }
        public DbSet<StarProject> StarProjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<ProjectModel>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .Entity<ProjectModel>()
                .HasOne(c => c.LastModifiedUser)
                .WithMany()
                .HasForeignKey(c => c.LastestVersionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .Entity<UserProject>()
                .HasOne(up => up.Editor)
                .WithMany(e => e.UserProjects)
                .HasForeignKey(up => up.EditorId);

            modelBuilder
                .Entity<UserProject>()
                .HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);
        }
    }
}
