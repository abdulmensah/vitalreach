using System.ComponentModel.DataAnnotations;

namespace VitalReach.Web.Data;

public sealed class AdminUser
{
    public int Id { get; set; }
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, MaxLength(254)] public string NormalizedEmail { get; set; } = "";
    [MaxLength(120)] public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(254)] public string CreatedBy { get; set; } = "";
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(254)] public string UpdatedBy { get; set; } = "";

    public static AdminUser Create(string email, string displayName, string actor) => new()
    {
        Email = email.Trim().ToLowerInvariant(), NormalizedEmail = email.Trim().ToUpperInvariant(),
        DisplayName = displayName.Trim(), IsActive = true, CreatedBy = actor, UpdatedBy = actor
    };
}
