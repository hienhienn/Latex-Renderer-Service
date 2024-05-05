namespace LatexRendererAPI.Services
{
  public interface IFileService
  {
    public Task<string> SaveFile(IFormFile file, string name, Guid projectId);
    public void DeleteFiles(string path);
  }

  public class FileService : IFileService
  {
    private string localPath = "";
    private IConfiguration config;

    public FileService(IConfiguration _config)
    {
      config = _config;
      localPath = Path.Combine(Directory.GetCurrentDirectory(), config["AssetPath"] ?? "");
    }
    public async Task<string> SaveFile(IFormFile file, string name, Guid projectId)
    {
      var fileExtension = Path.GetExtension(file.FileName);
      var newFileName = $"{name}_{DateTime.Now.Ticks}{fileExtension}";
      var fullPath = Path.Combine(localPath, projectId.ToString(), newFileName);
      using (FileStream stream = new FileStream(fullPath, FileMode.Create))
      {
        await file.CopyToAsync(stream);
        stream.Close();
      }
      return Path.Combine(projectId.ToString(), newFileName);
    }

    public void DeleteFiles(string path)
    {
      string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
      foreach (string file in files)
      {
        File.Delete(file);
      }
      Directory.Delete(path);
    }
  }
}
