using Microsoft.VisualBasic;

namespace LatexRendererAPI.Services
{
  public interface IFileService
  {
    public Task<string> SaveFile(IFormFile file, string name, Guid projectId, string filePath);
    public void DeleteFolder(string path);
    public void DeleteFile(string path, Guid projectId);
    public void DeleteFileAllVersion(string path);
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
    public async Task<string> SaveFile(IFormFile file, string name, Guid projectId, string filePath)
    {
      var fileExtension = Path.GetExtension(file.FileName);
      var newFileName = $"{name}_{DateTime.Now.Ticks}{fileExtension}";
      var fullPath = Path.Combine(localPath, projectId.ToString(), newFileName);
      using (FileStream stream = new FileStream(fullPath, FileMode.Create))
      {
        await file.CopyToAsync(stream);
        stream.Close();
      }

      var pathSplit = filePath.Split('/');
      string[] pathStr = [
        localPath,
        projectId.ToString(),
        config["CompilePath"] ?? ""
      ];
      string[] final = pathStr.Concat(pathSplit).ToArray();
      string[] folderPath = new string[final.Length - 1];
      Array.Copy(final, folderPath, final.Length - 1);
      Directory.CreateDirectory(Path.Combine(folderPath));

      using (FileStream stream = new FileStream(Path.Combine(final), FileMode.Create))
      {
        await file.CopyToAsync(stream);
        stream.Close();
      }

      return Path.Combine(projectId.ToString(), newFileName);
    }

    public void DeleteFolder(string path)
    {
      foreach (string filename in Directory.GetFiles(path))
      {
        File.Delete(filename);
      }
      // Check all child Directories and delete files  
      foreach (string subfolder in Directory.GetDirectories(path))
      {
        DeleteFolder(subfolder);
      }
      Directory.Delete(path);
    }

    public void DeleteFile(string path, Guid projectId)
    {
      var pathSplit = path.Split('/');
      string[] pathStr = [
        localPath,
        projectId.ToString(),
        config["CompilePath"] ?? ""
      ];
      string[] final = pathStr.Concat(pathSplit).ToArray();
      File.Delete(Path.Combine(final));
    }

    public void DeleteFileAllVersion(string path)
    {
      // var pathSplit = path.Split('/');
      // string[] pathStr = [localPath];
      // string[] final = pathStr.Concat(pathSplit).ToArray();
      // Console.WriteLine(Path.Combine(final));
      File.Delete(Path.Combine(localPath, path));
    }
  }
}
