#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class Contact
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;

    private ContactInput Input = new();
    private HeadquartersSettings? Headquarters;
    private string? StatusMessage;
    private bool IsError;
    private bool Submitting;

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        Headquarters = await db.Headquarters.AsNoTracking().SingleAsync(x => x.Id == 1);
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
