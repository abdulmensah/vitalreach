using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class HeadquartersSettings
{
    public int Id { get; set; } = 1;
    [Required, MaxLength(120)] public string CenterName { get; set; } = "VitalReach Shopping & Wellness Center";
    [Required, MaxLength(160)] public string AddressLine1 { get; set; } = "24 Kusia Street";
    [MaxLength(160)] public string AddressLine2 { get; set; } = "";
    [Required, MaxLength(100)] public string City { get; set; } = "Kokomlemle";
    [MaxLength(100)] public string Region { get; set; } = "Greater Accra";
    [MaxLength(30)] public string PostalCode { get; set; } = "";
    [Required, MaxLength(100)] public string Country { get; set; } = "Ghana";
    [Required, Phone, MaxLength(40)] public string Phone { get; set; } = "+1 (410) 504-4449";
    [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = "hello@vitalreachwellness.com";
    [MaxLength(240)] public string Hours { get; set; } = "Visits and shopping consultations by appointment.";
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public string CityRegion => string.Join(", ", new[] { City, Region }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string PhoneHref => $"tel:{new string(Phone.Where(x => char.IsDigit(x) || x == '+').ToArray())}";
    public string MapUrl => $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(string.Join(", ", new[] { AddressLine1, AddressLine2, City, Region, Country }.Where(x => !string.IsNullOrWhiteSpace(x))))}";
}
