using VitalReach.Web.Components;
using VitalReach.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;

LocalEnvironment.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
var builder = WebApplication.CreateBuilder(args);
var dataProtectionPath = builder.Configuration["DataProtection:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName(builder.Configuration["DataProtection:ApplicationName"] ?? "VitalReach.Local");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddDbContextFactory<CatalogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Catalog") ?? "Data Source=App_Data/vitalreach.db"));
builder.Services.AddSingleton<ProductImageStorage>();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleConfigured = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);
var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = googleConfigured ? GoogleDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "VitalReach.Admin";
    options.LoginPath = "/auth/login";
    options.AccessDeniedPath = "/auth/denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
if (googleConfigured)
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
    });
}
builder.Services.AddScoped<IAuthorizationHandler, DatabaseAdminAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", policy =>
    policy.RequireAuthenticatedUser().AddRequirements(new DatabaseAdminRequirement()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error", createScopeForErrors: true); app.UseHsts(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
var productImagesPath = ProductImageStorage.ResolveStoragePath(builder.Configuration, builder.Environment);
Directory.CreateDirectory(productImagesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(productImagesPath),
    RequestPath = "/uploads/products"
});
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
await CatalogSeeder.SeedAsync(app.Services);
if (googleConfigured)
{
    app.MapGet("/auth/login", (string? returnUrl) => Results.Challenge(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) },
        [GoogleDefaults.AuthenticationScheme]));
}
else
{
    app.MapGet("/auth/login", () => Results.Problem("Google authentication has not been configured.", statusCode: 503));
}
app.MapGet("/auth/denied", () => Results.Problem("This Google account is not authorized for VitalReach administration.", statusCode: 403));
app.MapGet("/auth/logout", () => Results.SignOut(
    new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" },
    [CookieAuthenticationDefaults.AuthenticationScheme]));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "vitalreach-qa" }));
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

static string SafeReturnUrl(string? value) =>
    !string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, UriKind.Relative, out _) && value.StartsWith('/') && !value.StartsWith("//") ? value : "/admin/products";
