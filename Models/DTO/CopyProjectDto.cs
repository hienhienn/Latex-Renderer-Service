using System.ComponentModel.DataAnnotations;

namespace LatexRendererAPI.Models.DTO
{
    public class CopyProjectDto
    {
        public required string Name { get; set; }
        public required Guid VersionId { get; set; }
    }
}