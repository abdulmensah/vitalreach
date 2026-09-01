#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class Contact
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;
    [Parameter, SupplyParameterFromQuery(Name = "message")] public string? PresetMessage { get; set; }

    private ContactInput Input = new();
    private HeadquartersSettings? Headquarters;
    private string? StatusMessage;
    private string? AppliedPresetMessage;
    private bool IsError;
    private bool Submitting;
    private string MapEmbedUrl => Headquarters is null
        ? "about:blank"
        : $"https://www.google.com/maps?q={Uri.EscapeDataString(string.Join(", ", new[] { Headquarters.AddressLine1, Headquarters.AddressLine2, Headquarters.City, Headquarters.Region, Headquarters.Country }.Where(value => !string.IsNullOrWhiteSpace(value))))}&output=embed";

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
    }

    protected override void OnParametersSet()
    {
        var message = PresetMessage?.Trim();
        if (string.IsNullOrWhiteSpace(message) || string.Equals(message, AppliedPresetMessage, StringComparison.Ordinal))
            return;

        Input.Message = message;
        AppliedPresetMessage = message;
        StatusMessage = null;
        IsError = false;
    }

    private async Task SubmitAsync()
    {
        Submitting = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(Input.Website))
            {
                ShowSuccess();
                return;
            }

            await using var db = await DbFactory.CreateDbContextAsync();
            db.ContactSubmissions.Add(new ContactSubmission
            {
                Name = Input.Name.Trim(),
                Email = Input.Email.Trim(),
                Phone = Input.Phone.Trim(),
                Message = Input.Message.Trim()
            });
            await db.SaveChangesAsync();
            ShowSuccess();
        }
        catch (DbUpdateException)
        {
            IsError = true;
            StatusMessage = "Your message could not be sent. Please try again or contact us by phone.";
        }
        finally
        {
            Submitting = false;
        }
    }

    private void ShowSuccess()
    {
        Input = new ContactInput();
        IsError = false;
        StatusMessage = "Thank you. Your message has been sent successfully, and the VitalReach team will follow up.";
    }

    private sealed class ContactInput
    {
        [Required, MaxLength(120)] public string Name { get; set; } = "";
        [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = "";
        [Required, Phone, MaxLength(40)] public string Phone { get; set; } = "";
        [Required, MinLength(10), MaxLength(4000)] public string Message { get; set; } = "";
        public string Website { get; set; } = "";
    }
}
