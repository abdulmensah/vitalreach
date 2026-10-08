using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public enum LocationStatus { Draft, Published, Archived }

public sealed class Location
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string CenterName { get; set; } = "";
    [Required, MaxLength(160)] public string AddressLine1 { get; set; } = "";
    [MaxLength(160)] public string AddressLine2 { get; set; } = "";
    [Required, MaxLength(100)] public string City { get; set; } = "";
    [MaxLength(100)] public string Region { get; set; } = "";
    [MaxLength(30)] public string PostalCode { get; set; } = "";
    [Required, MaxLength(100)] public string Country { get; set; } = "";
    [Phone, MaxLength(40)] public string? Phone { get; set; }
    [EmailAddress, MaxLength(180)] public string? Email { get; set; }
    [MaxLength(240)] public string Hours { get; set; } = "";
    [EnumDataType(typeof(LocationStatus))] public LocationStatus Status { get; set; } = LocationStatus.Draft;
    [Range(0, 100000)] public int Rank { get; set; } = 100;
    public int Version { get; set; }
    public string CityRegion => string.Join(", ", new[] { City, Region }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string PhoneHref => $"tel:{new string((Phone ?? "").Where(x => char.IsDigit(x) || x == '+').ToArray())}";
    private string MapQuery => Uri.EscapeDataString(string.Join(", ", new[] { AddressLine1, AddressLine2, City, Region, PostalCode, Country }.Where(x => !string.IsNullOrWhiteSpace(x))));
    public string MapUrl => $"https://www.google.com/maps/search/?api=1&query={MapQuery}";
    public string MapEmbedUrl => $"https://www.google.com/maps?q={MapQuery}&output=embed";
}
