using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class ClinicalReviewerRequirement : IAuthorizationRequirement;

public sealed class ClinicalReviewerAuthorizationHandler(IDbContextFactory<CatalogDbContext> factory)
    : AuthorizationHandler<ClinicalReviewerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ClinicalReviewerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var email = context.User.FindFirstValue(ClaimTypes.Email);
        if (email is null) return;
        await using var db = await factory.CreateDbContextAsync();
        if (await db.AdminUsers.AnyAsync(x => x.IsActive && x.IsClinicalReviewer && x.NormalizedEmail == email.ToUpper())) context.Succeed(requirement);
    }
}

public sealed class SuperAdminRequirement : IAuthorizationRequirement;
public sealed class SuperAdminAuthorizationHandler(IDbContextFactory<CatalogDbContext> factory) : AuthorizationHandler<SuperAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperAdminRequirement requirement)
    {
        var email = context.User.FindFirstValue(ClaimTypes.Email);
        if (context.User.Identity?.IsAuthenticated != true || email is null) return;
        await using var db = await factory.CreateDbContextAsync();
        if (await db.AdminUsers.AnyAsync(x => x.IsActive && x.IsSuperAdmin && x.NormalizedEmail == email.ToUpper())) context.Succeed(requirement);
    }
}
