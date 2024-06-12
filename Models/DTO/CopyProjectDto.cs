using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
    public class CopyProjectDto
    {
        [Required]
        public required SaveFileVersionDto[] Files { get; set; }
        public required string Name { get; set; }
        public required string MainFilePath { get; set; }
    }
}