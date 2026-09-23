#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VitalReach.Web.Data;

public sealed class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Version { get; set; }
    [Required, MaxLength(80)] public string Sku { get; set; } = "";
    [Required, MaxLength(180)] public string Name { get; set; } = "Standard";
    [Range(typeof(decimal), "0", "100000")] public decimal Price { get; set; }
    // Null stock means availability must be confirmed, not zero or unlimited inventory.
    [Range(0, int.MaxValue)] public int? StockQuantity { get; set; }
    public bool IsAvailable { get; set; } = true;
    [MaxLength(60)] public string Format { get; set; } = "";
    [Range(1, int.MaxValue)] public int? Count { get; set; }
    [MaxLength(60)] public string Color { get; set; } = "";
    [MaxLength(60)] public string Size { get; set; } = "";
    [MaxLength(60)] public string Flavor { get; set; } = "";
    [MaxLength(60)] public string Material { get; set; } = "";
    [MaxLength(80)] public string Strength { get; set; } = "";
    [Range(typeof(decimal), "0.001", "1000000")] public decimal? NetContent { get; set; }
    [MaxLength(10)] public string NetContentUnit { get; set; } = "";
    [Range(typeof(decimal), "0.001", "1000000")] public decimal? ShippingWeightGrams { get; set; }
    [MaxLength(500), RegularExpression(@"^(https?://|/)[^\s]+$", ErrorMessage = "Use an https:// URL or a site-relative path beginning with /.")]
    public string? ImageUrl { get; set; }
    [NotMapped] public bool CanOrder => IsAvailable && StockQuantity is not 0;
    [NotMapped] public string Attributes => string.Join(" · ", new[]
    {
        Format, Count.HasValue ? $"{Count} count" : "", Strength, Color, Size, Flavor, Material,
        NetContent.HasValue ? $"{NetContent:0.###} {NetContentUnit}" : ""
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
    [NotMapped] public string DisplayName => string.IsNullOrEmpty(Attributes) ? Name : $"{Name} — {Attributes}";
    public ProductVariant Copy() => (ProductVariant)MemberwiseClone();
}
