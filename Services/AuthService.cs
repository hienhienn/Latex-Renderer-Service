using LatexRendererAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LatexRendererAPI.Services
{
    public class AuthService
    {
        private AppDbContext dbContext;
        public AuthService(AppDbContext _dbContext)
        {
           dbContext = _dbContext;
        }

        public async Task<bool> CheckViewPermission(Guid userId, Guid projectId)
        { 
            var project = await dbContext.Projects.Include(p => p.UserProjects).FirstAsync(p => p.Id == projectId);
            if(project.IsPublic == true) return true;
            if(project.UserProjects.FirstOrDefault(up => up.EditorId == userId) == null)
            {
                return false;
            }
            return true;

        }

        public bool CheckEditPermission(Guid userId, Guid projectId)   
        {
            var userProject = dbContext.UserProjects.FirstOrDefault(up => up.ProjectId == projectId && up.EditorId == userId);
            if (userProject == null) return false;
            if (userProject.Role == "viewer") return false;
            return true;
        }

        public bool CheckOwnerPermission(Guid userId, Guid projectId)
        {
            var userProject = dbContext.UserProjects.FirstOrDefault(up => up.ProjectId == projectId && up.EditorId == userId);
            if (userProject == null) return false;
            if (userProject.Role == "owner") return true;
            return false;
        }
    }
}
