#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class AdminDashboard
{
    [Inject] private IDbContextFactory<CatalogDbContext> DbFactory { get; set; } = default!;

    private int ProductCount { get; set; }
    private int PublishedProductCount { get; set; }
    private int MessageCount { get; set; }
    private int UnreadMessageCount { get; set; }
    private int ActiveAdminCount { get; set; }
    private string HeadquartersStatus { get; set; } = "Not set";

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        ProductCount = await db.Products.CountAsync();
        PublishedProductCount = await db.Products.CountAsync(product => product.IsPublished);
        MessageCount = await db.ContactSubmissions.CountAsync();
        UnreadMessageCount = await db.ContactSubmissions.CountAsync(message => !message.IsRead);
        ActiveAdminCount = await db.AdminUsers.CountAsync(user => user.IsActive);
        HeadquartersStatus = await db.Headquarters.AnyAsync() ? "Configured" : "Not set";
    }
}
