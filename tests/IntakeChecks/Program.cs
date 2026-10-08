using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VitalReach.Web.Data;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
async Task Denied(Func<Task> action, string message)
{ try { await action(); } catch (UnauthorizedAccessException) { checks++; return; } throw new Exception(message); }
IntakeEntry Entry() => new() { Name = "Synthetic Patient Only", Age = 45, Pregnancy = "No / not applicable", Consent = true, Forms = ["diabetes", "kidney", "bp", "cardio", "liver", "mental"] };
var today = DateTime.UtcNow.Date;
Check(IntakePresentation.Color(0) == "blue" && IntakePresentation.Color(1) == "yellow" && IntakePresentation.Color(2) == "orange" && IntakePresentation.Color(3) == "red", "urgency palette");
Check(IntakePresentation.StatusColor("Reviewed") == "blue" && IntakePresentation.StatusColor(IntakePresentation.ClearedStatus) == "green", "green requires explicit clinical clearance");
bool Rule(IntakeEntry e, string rule) => IntakeScreening.Analyze(e, today).Any(f => f.Rule == rule);
var e = Entry();
Check(IntakeValidation.Errors(e, today).Count == 0, "valid minimal entry");
Check(Rule(e, "incomplete"), "unknowns explicitly incomplete");
Check(IntakeScreening.Analyze(e, today).Max(f => f.Priority) == 0, "missing results not abnormal values");
e.Consent = false; Check(IntakeValidation.Errors(e, today).Count > 0, "consent required"); e.Consent = true;
e.Age = 17; Check(IntakeValidation.Errors(e, today).Count > 0, "pediatric self-service blocked"); e.Age = 45;
e.Forms = []; Check(IntakeValidation.Errors(e, today).Count > 0, "form choice required"); e = Entry();
e.Answers["diabetes.s.1"] = "Maybe"; Check(IntakeValidation.Errors(e, today).Count > 0, "invalid answer rejected"); e = Entry();
foreach (var q in IntakeCatalog.Safety)
{ e.Answers[q.Id] = "Yes"; Check(IntakeScreening.Analyze(e, today).Any(f => f.Rule == q.Id && f.Priority == 3), q.Id + " emergency"); e.Answers.Clear(); }
e.Answers["cardio.e.2"] = "Yes"; Check(IntakeScreening.Analyze(e, today).Any(f => f.Rule == "cardio.e.2" && f.Priority == 3), "current stroke emergency"); e.Answers.Clear();
e.Answers["cardio.s.1"] = "Yes"; Check(IntakeScreening.Analyze(e, today).All(f => f.Priority != 3), "recent symptom not falsely asserted current"); e.Answers.Clear();
e.Answers["mental.s.10"] = "Yes"; Check(Rule(e, "mental.safety"), "past fortnight selfharm needs safety assessment"); e.Answers.Clear();
e.Answers["liver.t.1"] = "Yes"; Check(!Rule(e, "liver.jaundice"), "inspection alone is not a positive finding"); e.Answers.Clear();
e.Answers["liver.s.1"] = "Yes"; Check(Rule(e, "liver.jaundice"), "jaundice flag"); e.Answers.Clear();
e.BloodPressure[0] = new() { Date = today, Systolic = 180, Diastolic = 80 }; Check(Rule(e, "bp.severe"), "systolic severe boundary");
e.BloodPressure[0] = new() { Date = today, Systolic = 130, Diastolic = 120 }; Check(Rule(e, "bp.severe"), "diastolic severe boundary");
e.BloodPressure[0] = new() { Date = today.AddDays(-10), Systolic = 190, Diastolic = 100 }; Check(IntakeScreening.Analyze(e, today).Single(f => f.Rule == "bp.severe").Title.Contains("previous"), "historical severe result labeled");
e.BloodPressure[0] = new() { Date = today, Systolic = 139, Diastolic = 89 }; Check(!Rule(e, "bp.high"), "below checklist BP threshold");
e.BloodPressure[0].Diastolic = 90; Check(Rule(e, "bp.high"), "BP uses OR");
e.Pregnancy = "Yes"; Check(Rule(e, "scope") && !Rule(e, "bp.high"), "pregnancy individual interpretation"); e = Entry();
e.Glucose[0] = new() { Date = today, Value = 7m, Unit = "mmol/L", Type = "Fasting" }; Check(Rule(e, "glucose.high"), "mmol conversion 7 equals126");
e.Glucose[0].Value = 6.9m; Check(Rule(e, "glucose.raised") && !Rule(e, "glucose.high"), "fasting threshold below126");
e.Glucose[0] = new() { Date = today, Value = 126, Type = "Random" }; Check(!Rule(e, "glucose.high"), "random126 not fasting diagnostic threshold");
e.Glucose[0].Value = 200; Check(Rule(e, "glucose.high"), "random200 review");
e.Glucose[0].Value = 300; Check(Rule(e, "glucose.veryhigh"), "300 urgent");
e.Glucose[0].Value = 69; Check(Rule(e, "glucose.low"), "low glucose");
e.Glucose[0].Value = 70; Check(!Rule(e, "glucose.low"), "70 boundary");
e.Glucose[0].Date = today.AddDays(1); Check(IntakeValidation.Errors(e, today).Count > 0, "future lab rejected"); e = Entry();
e.Egfr = 59; e.EgfrDate = today; Check(Rule(e, "kidney.egfr"), "eGFR59"); e.Egfr = 60; Check(!Rule(e, "kidney.egfr"), "eGFR60");
e.Uacr = 30; e.UacrDate = today; Check(Rule(e, "kidney.uacr"), "uACR30");
e.Hba1c = 5.7m; e.Hba1cDate = today; Check(Rule(e, "a1c"), "HbA1c5.7");
e.Hba1cDate = null; Check(IntakeValidation.Errors(e, today).Count > 0, "result needs date");
Check(IntakeCatalog.Forms.Length == 6 && IntakeCatalog.Forms.SelectMany(f => f.Sections).SelectMany(s => s.Questions).Select(q => q.Id).Distinct().Count() == IntakeCatalog.Forms.SelectMany(f => f.Sections).SelectMany(s => s.Questions).Count(), "six forms with unique question IDs");

var dbPath = Path.Combine(Path.GetTempPath(), "vitalreach-intake-check-" + Guid.NewGuid() + ".db");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Intake:Enabled"] = "true", ["Intake:ReviewerEmails"] = "clinician@example.test", ["Intake:PublicUrl"] = "https://example.test" }).Build();
var auth = new TestAuthentication();
var services = new ServiceCollection();
services.AddLogging(); services.AddSingleton<IConfiguration>(config);
services.AddDbContextFactory<CatalogDbContext>(o => o.UseSqlite("Data Source=" + dbPath));
services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
services.AddSingleton<AuthenticationStateProvider>(auth);
services.AddAuthorizationCore(o =>
{
    o.AddPolicy("ClinicalReviewer", p => p.RequireAuthenticatedUser().AddRequirements(new ClinicalReviewerRequirement()));
    o.AddPolicy("SuperAdmin", p => p.RequireAuthenticatedUser().AddRequirements(new SuperAdminRequirement()));
});
services.AddScoped<IAuthorizationHandler, ClinicalReviewerAuthorizationHandler>();
services.AddScoped<IAuthorizationHandler, SuperAdminAuthorizationHandler>();
services.AddSingleton<IntakeSubmissionLimiter>();
services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
services.AddScoped<IntakeService>();
await using var provider = services.BuildServiceProvider();
try
{
    await using var scope = provider.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
    // Upgrade path: intake schema can create tables on a database that already exists; repeat is safe.
    await using (var db = await factory.CreateDbContextAsync())
    {
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE ConsultationSubmissions; DROP TABLE ConsultationAudits;");
        await IntakeSchema.EnsureAsync(db); await IntakeSchema.EnsureAsync(db);
        var clinician = AdminUser.Create("clinician@example.test", "Test clinician", "test"); clinician.IsClinicalReviewer = true; db.AdminUsers.Add(clinician);
        db.AdminUsers.Add(AdminUser.Create("admin@example.test", "Ordinary admin", "test"));
        await db.SaveChangesAsync();
        Check(await db.ConsultationSubmissions.CountAsync() == 0, "idempotent upgrade schema");
    }
    var service = scope.ServiceProvider.GetRequiredService<IntakeService>();
    var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    Check(service.PublicUrl == "https://example.test/consultation-intake", "QR uses configured canonical URL");
    var entry = Entry(); var id = await service.SubmitAsync(entry);
    Check(await service.SubmitAsync(entry) == id, "idempotent retry");
    await using (var db = await factory.CreateDbContextAsync())
    {
        var stored = await db.ConsultationSubmissions.SingleAsync();
        Check(!stored.ProtectedContent.Contains(entry.Name) && !stored.ProtectedContent.Contains("diabetes"), "answers encrypted");
        Check(await db.ConsultationSubmissions.CountAsync() == 1, "retry stores one row");
    }
    await Denied(() => service.ListAsync("All", 0), "anonymous list must fail");
    auth.Login("admin@example.test");
    Check(!(await authorization.AuthorizeAsync((await auth.GetAuthenticationStateAsync()).User, "SuperAdmin")).Succeeded, "ordinary admin cannot manage roles");
    await Denied(() => service.ReadAsync(id), "ordinary admin detail must fail");
    await Denied(() => service.ReviewAsync(id, Guid.NewGuid(), "Reviewed", "test"), "ordinary admin writes must fail");
    auth.Login("clinician@example.test");
    Check(!(await authorization.AuthorizeAsync((await auth.GetAuthenticationStateAsync()).User, "SuperAdmin")).Succeeded, "clinical reviewer is not automatically superadmin");
    var list = await service.ListAsync("All", 0); Check(list.Count == 1 && list[0].ProtectedContent == "", "list metadata only");
    var detail = (await service.ReadAsync(id))!; Check(detail.Content.Entry.Name == entry.Name, "authorized decryption");
    Check(detail.Content.FormVersion == "1" && detail.Content.ConsentedUtc <= DateTime.UtcNow, "versioned consent");
    Check(detail.Content.PrivacyVersion == LegalPolicyVersion.Current && detail.Content.TermsVersion == LegalPolicyVersion.Current,
        "submitted envelope records policies shown");
    var historicalJson = System.Text.Json.JsonSerializer.SerializeToNode(detail.Content)!.AsObject();
    historicalJson.Remove("PrivacyVersion"); historicalJson.Remove("TermsVersion");
    var historical = System.Text.Json.JsonSerializer.Deserialize<IntakeEnvelope>(historicalJson.ToJsonString())!;
    Check(string.IsNullOrEmpty(historical.PrivacyVersion) && string.IsNullOrEmpty(historical.TermsVersion),
        "historical envelopes do not claim new policy versions");
    await service.ReviewAsync(id, detail.Record.Version, "Reviewed", "Synthetic clinical plan");
    try { await service.ReviewAsync(id, detail.Record.Version, "Reviewed", "stale"); throw new Exception("stale update accepted"); } catch (InvalidOperationException) { checks++; }
    Check((await service.ReadAsync(id))!.Review!.Notes == "Synthetic clinical plan", "review stored");
    var reviewed = (await service.ReadAsync(id))!;
    await service.ReviewAsync(id, reviewed.Record.Version, IntakePresentation.ClearedStatus, "Clinician confirms no outstanding action after assessment.");
    var cleared = (await service.ReadAsync(id))!;
    Check(cleared.Record.Priority == reviewed.Record.Priority && cleared.Content.Flags.SequenceEqual(reviewed.Content.Flags), "clearance preserves original flags");
    Check((await service.ListAsync(IntakePresentation.ClearedStatus, 0)).Count == 1, "clearance status filter");
    await using (var db = await factory.CreateDbContextAsync())
    {
        Check(await db.ConsultationAudits.CountAsync() >= 4, "reads and writes audited");
        Check(!(await db.ConsultationSubmissions.SingleAsync()).ProtectedReview.Contains("Synthetic"), "review encrypted");
        (await db.AdminUsers.SingleAsync(a => a.Email == "clinician@example.test")).IsActive = false; await db.SaveChangesAsync();
    }
    await Denied(() => service.ListAsync("All", 0), "revocation applies inside active service");
    config["Intake:Enabled"] = "false";
    try { await service.SubmitAsync(Entry()); throw new Exception("disabled accepted"); } catch (InvalidOperationException) { checks++; }
    config["Intake:PublicUrl"] = "http://example.test"; Check(service.PublicUrl is null, "QR requires https");
    await using (var db = await factory.CreateDbContextAsync())
    {
        db.AdminUsers.Add(AdminUser.Create("abdulmensah@gmail.com", "Owner", "test"));
        db.AdminUsers.Add(AdminUser.Create("masaoudaa@gmail.com", "Reviewer", "test"));
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE SeedHistory (Key TEXT PRIMARY KEY, AppliedUtc TEXT NOT NULL);");
        await AdminAccessSchema.EnsureAsync(db);
        await AdminAccessSchema.BootstrapAsync(db);
        db.ChangeTracker.Clear();
        Check(await db.AdminUsers.CountAsync(a => a.IsClinicalReviewer) == 3, "specified initial reviewers granted");
        Check(await db.AdminUsers.CountAsync(a => a.IsSuperAdmin) == 1, "only Abdul bootstrapped superadmin");
        var owner = await db.AdminUsers.SingleAsync(a => a.Email == "abdulmensah@gmail.com");
        owner.IsClinicalReviewer = false; await db.SaveChangesAsync();
        await AdminAccessSchema.BootstrapAsync(db); db.ChangeTracker.Clear();
        Check(!await db.AdminUsers.AnyAsync(a => a.Email == "abdulmensah@gmail.com" && a.IsClinicalReviewer), "revocation survives startup");
        auth.Login("abdulmensah@gmail.com");
        Check((await authorization.AuthorizeAsync((await auth.GetAuthenticationStateAsync()).User, "SuperAdmin")).Succeeded, "owner can manage roles");
        await Denied(() => service.ReadAsync(id), "superadmin without clinical role cannot read health data");
    }
    using var qrData = QRCoder.QRCodeGenerator.GenerateQrCode("https://example.test/consultation-intake", QRCoder.QRCodeGenerator.ECCLevel.Q);
    using var qr = new QRCoder.SvgQRCode(qrData); Check(qr.GetGraphic(10).Contains("<svg"), "QR SVG generated");
    using var limiter = new IntakeSubmissionLimiter();
    Check(Enumerable.Range(0, 20).All(_ => limiter.TryAcquire("test")) && !limiter.TryAcquire("test"), "hourly limiter");
    Console.WriteLine($"PASS: {checks} consultation checks.");
}
finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(dbPath); }

sealed class TestAuthentication : AuthenticationStateProvider
{
    private ClaimsPrincipal user = new(new ClaimsIdentity());
    public void Login(string email) => user = new(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "Test"));
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
}

