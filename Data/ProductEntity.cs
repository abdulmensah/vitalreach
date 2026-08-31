using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class ProductEntity
{
    public int Id { get; set; }
    [Required, MaxLength(80)] public string Slug { get; set; } = "";
    [Required, MaxLength(140)] public string Name { get; set; } = "";
    [Range(0, 100000)] public decimal Price { get; set; }
    [Required, MaxLength(80)] public string Category { get; set; } = "";
    [Required, MaxLength(180)] public string Benefit { get; set; } = "";
    [MaxLength(80)] public string Detail { get; set; } = "";
    [MaxLength(40)] public string Theme { get; set; } = "teal-product";
    [MaxLength(30)] public string Orb { get; set; } = "";
    [MaxLength(50)] public string LabelOne { get; set; } = "";
    [MaxLength(50)] public string LabelTwo { get; set; } = "";
    [MaxLength(500), RegularExpression(@"^(https?://|/)[^\s]+$", ErrorMessage = "Use an https:// URL or a site-relative path beginning with /.")]
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
