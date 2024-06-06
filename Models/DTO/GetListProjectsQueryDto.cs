using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LatexRendererAPI.Models.DTO
{
  public class GetListProjectsQueryDto
  {
    [DefaultValue(1)]
    public int Page { get; set; } = 1;

    [DefaultValue(10)]
    public int PageSize { get; set; } = 10;

    [DefaultValue("all")]
    [AllowedValues(["all", "yours", "shared", "starred"])]
    [AllowNull]
    public string Category { get; set; } = "all";

    [AllowedValues(["name", "modifiedTime", null])]
    public string? FieldSort { get; set; }

    [DefaultValue("descend")]
    [AllowedValues(["ascend", "descend"])]
    public string? Sort { get; set; } = "descend";

    [AllowNull]
    [DefaultValue("")]
    public string? Keyword { get; set; }
  }
}