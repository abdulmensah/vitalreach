using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class DatabaseAdminRequirement : IAuthorizationRequirement;

public sealed class DatabaseAdminAuthorizationHandler(IDbContextFactory<CatalogDbContext> factory)
    : AuthorizationHandler<DatabaseAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DatabaseAdminRequirement requirement)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return;
        await using var db = await factory.CreateDbContextAsync();
        if (await db.AdminUsers.AsNoTracking().AnyAsync(x => x.NormalizedEmail == email.ToUpper() && x.IsActive))
            context.Succeed(requirement);
    }
}
