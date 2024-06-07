namespace LatexRendererAPI.Services
{
  public interface IFileService
  {
    public Task<string> SaveFile(IFormFile file, string name, string filePath);
    public void DeleteFolder(string path);
    // public void DeleteFile(string path, Guid projectId);
    public void DeleteFileRelativePath(string path);
    public void DeleteFile(string path);
    public string ParseFolderPath(string filePath, string code, string typePath);
    public void CopyFile(string source, string des);
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
    public async Task<string> SaveFile(IFormFile file, string name, string filePath)
    {
      var fileExtension = Path.GetExtension(file.FileName);
      var newFileName = $"{name}_{DateTime.Now.Ticks}{fileExtension}";
      var fullPath = Path.Combine(localPath, newFileName);
      using (FileStream stream = new FileStream(fullPath, FileMode.Create))
      {
        await file.CopyToAsync(stream);
        stream.Close();
      }

      return newFileName;
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

    // public void DeleteFile(string path, Guid projectId)
    // {
    //   var pathSplit = path.Split('/');
    //   string[] pathStr = [
    //     localPath,
    //     projectId.ToString(),
    //     config["CompilePath"] ?? ""
    //   ];
    //   string[] final = pathStr.Concat(pathSplit).ToArray();
    //   File.Delete(Path.Combine(final));
    // }

    public void DeleteFileRelativePath(string path)
    {
      File.Delete(Path.Combine(localPath, path));
    }

    public void DeleteFile(string path)
    {
      File.Delete(path);
    }


    public string ParseFolderPath(string filePath, string code, string typePath) {
      var pathSplit = filePath.Split('/');
      string[] pathStr = [
        localPath,
        code,
      ];
      string[] final = pathStr.Concat(pathSplit).ToArray();
      if(typePath == "file") 
      {
        string[] folderPath = new string[final.Length - 1];
        Array.Copy(final, folderPath, final.Length - 1);
        return Path.Combine(folderPath);
      }
      else return Path.Combine(final);
    }

     public void CopyFile(string source, string des) {
      File.Copy(source, des);
     }
  }
}
