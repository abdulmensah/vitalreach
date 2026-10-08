using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class ContactSubmission
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = "";
    [Required, Phone, MaxLength(40)] public string Phone { get; set; } = "";
    [Required, MinLength(10), MaxLength(4000)] public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
