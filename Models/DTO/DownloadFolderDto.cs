using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
  public class DownloadFolderDto
  {
    [Required]
    public required SaveFileVersionDto[] Files { get; set; }

    [Required]
    public required string Code { get; set; }

    [Required]
    public required string FolderPath { get; set; }

    [Required]
    public required string FolderName { get; set; }
  }
}