using Microsoft.EntityFrameworkCore;
using LatexRendererAPI.Models.Domain;

namespace LatexRendererAPI.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }
    public DbSet<UserModel> Users { get; set; }

    public DbSet<ProjectModel> Projects { get; set; }
    public DbSet<VersionModel> Versions { get; set; }
    public DbSet<FileModel> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ProjectModel>()
                  .HasOne(c => c.Owner)
                  .WithMany()
                  .HasForeignKey(c => c.OwnerId)
                  .OnDelete(DeleteBehavior.NoAction);
    }
  }
}