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
  }
}