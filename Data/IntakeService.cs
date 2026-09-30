using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public sealed class IntakeService(IDbContextFactory<CatalogDbContext> factory, IDataProtectionProvider protection,
    IConfiguration configuration, AuthenticationStateProvider authentication, IAuthorizationService authorization,
    IntakeSubmissionLimiter limiter, IHttpContextAccessor httpContext)
{
    private readonly string clientAddress = httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    private readonly IDataProtector protector = protection.CreateProtector("VitalReach.Consultation.v1");
    private DateTime lastSubmission;
    public bool Enabled => configuration.GetValue<bool>("Intake:Enabled");
    public string? PublicUrl
    {
        get
        {
            var value = configuration["Intake:PublicUrl"];
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.UserInfo)
                ? new Uri(uri, "/consultation-intake").AbsoluteUri : null;
        }
    }

    public async Task<Guid> SubmitAsync(IntakeEntry entry)
    {
        if (!Enabled) throw new InvalidOperationException("Online intake is not open. Please speak to center staff.");
        var now = DateTime.UtcNow;
        var errors = IntakeValidation.Errors(entry, now);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
        // Retries use the same random request key. No public read endpoint exposes patient data.
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.AdminUsers.AnyAsync(x => x.IsActive && x.IsClinicalReviewer)) throw new InvalidOperationException("No clinical reviewer is available. Please speak to center staff.");
        if (await db.ConsultationSubmissions.AnyAsync(x => x.Id == entry.SubmissionId)) return entry.SubmissionId;
        if (now - lastSubmission < TimeSpan.FromMinutes(1)) throw new InvalidOperationException("Please wait a minute before submitting another intake.");
        if (!limiter.TryAcquire(clientAddress)) throw new InvalidOperationException("This connection has reached its hourly intake limit. Please ask center staff for assistance.");
        lastSubmission = now;
        var flags = IntakeScreening.Analyze(entry, now);
        db.ConsultationSubmissions.Add(new ConsultationSubmission
        {
            Id = entry.SubmissionId, CreatedUtc = now, Priority = flags.Max(f => f.Priority),
            ProtectedContent = protector.Protect(JsonSerializer.Serialize(new IntakeEnvelope(entry, flags, now)))
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // Concurrent double submission is safe only when this exact request ID exists.
            await using var retry = await factory.CreateDbContextAsync();
            if (!await retry.ConsultationSubmissions.AnyAsync(x => x.Id == entry.SubmissionId)) throw;
        }
        return entry.SubmissionId;
    }

    private async Task<string> RequireReviewerAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (!(await authorization.AuthorizeAsync(user, null, "ClinicalReviewer")).Succeeded) throw new UnauthorizedAccessException("Clinical reviewer access is required.");
        return user.FindFirstValue(ClaimTypes.Email)!;
    }

    public async Task<List<ConsultationSubmission>> ListAsync(string status, int page)
    {
        var reviewer = await RequireReviewerAsync();
        await using var db = await factory.CreateDbContextAsync();
        var query = db.ConsultationSubmissions.AsNoTracking();
        if (status is "Awaiting review" or "In review" or "Reviewed") query = query.Where(x => x.Status == status);
        // Project metadata only. Full answers are decrypted only after a separately authorized, audited detail read.
        var result = await query.OrderByDescending(x => x.Priority).ThenByDescending(x => x.CreatedUtc).ThenBy(x => x.Id)
            .Skip(Math.Max(0, page) * 25).Take(25).Select(x => new ConsultationSubmission
            { Id = x.Id, CreatedUtc = x.CreatedUtc, Priority = x.Priority, Status = x.Status, Version = x.Version }).ToListAsync();
        db.ConsultationAudits.Add(Audit(reviewer, "List", null));
        await db.SaveChangesAsync();
        return result;
    }

    public async Task<IntakeDetail?> ReadAsync(Guid id)
    {
        var reviewer = await RequireReviewerAsync();
        await using var db = await factory.CreateDbContextAsync();
        var item = await db.ConsultationSubmissions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return null;
        db.ConsultationAudits.Add(Audit(reviewer, "Read", id));
        await db.SaveChangesAsync();
        return new(item, JsonSerializer.Deserialize<IntakeEnvelope>(protector.Unprotect(item.ProtectedContent))!,
            item.ProtectedReview.Length == 0 ? null : JsonSerializer.Deserialize<ClinicalReview>(protector.Unprotect(item.ProtectedReview)));
    }

    public async Task ReviewAsync(Guid id, Guid version, string status, string notes)
    {
        var reviewer = await RequireReviewerAsync();
        if (status is not ("In review" or "Reviewed") || notes.Length > 4000 || string.IsNullOrWhiteSpace(notes)) throw new InvalidOperationException("Choose a review status and enter a clinical assessment / action note (up to 4,000 characters).");
        await using var db = await factory.CreateDbContextAsync();
        var item = await db.ConsultationSubmissions.SingleAsync(x => x.Id == id);
        if (item.Version != version) throw new InvalidOperationException("Another reviewer changed this entry. Reopen it before saving.");
        item.Status = status;
        item.ProtectedReview = protector.Protect(JsonSerializer.Serialize(new ClinicalReview(status, notes, reviewer, DateTime.UtcNow)));
        item.Version = Guid.NewGuid();
        db.ConsultationAudits.Add(Audit(reviewer, "Review updated", id));
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Another reviewer changed this entry. Reopen it before saving."); }
    }
    private static ConsultationAudit Audit(string reviewer, string action, Guid? id) => new() { Reviewer = reviewer, Action = action, SubmissionId = id, CreatedUtc = DateTime.UtcNow };
}
