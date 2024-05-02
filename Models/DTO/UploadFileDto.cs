namespace LatexRendererAPI.Models.DTO
{
  public class UploadFileDto
  {
    public required Guid VersionId;
    public required string Name;
    public required string Path;
    public required IFormFile File;
  }
}