using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public ProductEntity Product { get; set; } = default!;
    [Required, MaxLength(500)] public string ImageUrl { get; set; } = "";
    [Required, MaxLength(180)] public string AltText { get; set; } = "";
    public int SortOrder { get; set; }
}
