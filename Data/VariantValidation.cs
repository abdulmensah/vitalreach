using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public static class VariantValidation
{
    public static readonly string[] ContentUnits = ["g", "kg", "mg", "ml", "L", "oz", "fl oz"];

    public static string? Validate(IReadOnlyCollection<ProductVariant> variants)
    {
        if (variants.Count == 0) return "Add at least one variant before saving the product.";
        foreach (var variant in variants)
        {
            variant.Sku = variant.Sku.Trim().ToUpperInvariant();
            variant.Name = variant.Name.Trim();
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(variant, new ValidationContext(variant), errors, true))
                return $"Variant “{variant.Name}”: {errors[0].ErrorMessage}";
            if (variant.Sku.Any(char.IsWhiteSpace)) return "Variant SKUs cannot contain spaces.";
            if (decimal.Round(variant.Price, 2) != variant.Price) return "Variant prices must have at most two decimal places.";
            if (variant.NetContent.HasValue != !string.IsNullOrEmpty(variant.NetContentUnit))
                return "Enter both the net contents amount and its unit, or leave both empty.";
            if (variant.NetContent.HasValue && !ContentUnits.Contains(variant.NetContentUnit))
                return "Choose a supported net contents unit.";
        }
        if (variants.Select(v => v.Sku).Distinct(StringComparer.OrdinalIgnoreCase).Count() != variants.Count)
            return "Each variant must have a unique SKU.";
        return null;
    }
}
