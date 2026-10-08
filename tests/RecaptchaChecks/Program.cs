using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VitalReach.Web.Data;
using VitalReach.Web.Components.Pages;

int checks = 0;
void Check(bool pass, string name) { if (!pass) throw new Exception(name); checks++; }
var envPrefix = "VitalReachConfigCheck_" + Guid.NewGuid().ToString("N");
var envFile = Path.GetTempFileName();
try
{
    File.WriteAllText(envFile, $"{envPrefix}__Secret=\n{envPrefix}__Quoted=\"\"\n{envPrefix}__Model=gpt-5-nano\n{envPrefix}__Existing=file-value\n");
    System.Environment.SetEnvironmentVariable(envPrefix + "__Existing", "host-value");
    LocalEnvironment.Load(envFile);
    var resolved = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Secret"] = "managed-secret", ["Quoted"] = "managed-secret" })
        .AddEnvironmentVariables(envPrefix + "__").Build();
    Check(resolved["Secret"] == "managed-secret" && resolved["Quoted"] == "managed-secret", "blank env placeholders preserve managed secrets");
    Check(resolved["Model"] == "gpt-5-nano", "nonempty env settings load");
    Check(resolved["Existing"] == "host-value", "host environment takes precedence");
}
finally
{
    File.Delete(envFile);
    foreach (var name in new[] { "Secret", "Quoted", "Model", "Existing" })
        System.Environment.SetEnvironmentVariable(envPrefix + "__" + name, null);
}
var handler = new FakeGoogle();
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
    ["Recaptcha:SiteKey"]="test-site", ["Recaptcha:SecretKey"]="test-secret", ["Recaptcha:AllowedHosts"]="test.vitalreachhub.com"
}).Build();
var environment = new EnvironmentStub();
var verifier = new RecaptchaService(new HttpClient(handler), config, environment);
Check(!await verifier.VerifyAsync(null, "contact") && handler.Calls == 0, "missing token blocked without provider request");
Check(!await verifier.VerifyAsync(new string('x', 8193), "contact") && handler.Calls == 0, "oversized token blocked");
Check(await verifier.VerifyAsync("test-token", "contact"), "valid response accepted");
Check(handler.RequestBody.Contains("secret=test-secret") && handler.RequestBody.Contains("response=test-token"), "secret and token sent via POST body");
handler.Action = "newsletter";
Check(!await verifier.VerifyAsync("test-token", "contact"), "wrong action blocked");
handler.Action = "contact"; handler.Score = 0.49;
Check(!await verifier.VerifyAsync("test-token", "contact"), "low score blocked");
handler.Score = null;
Check(!await verifier.VerifyAsync("test-token", "contact"), "missing score blocked");
Check(verifier.DevelopmentFailure is null, "diagnostic details hidden outside Development");
environment.EnvironmentName = "Development";
handler.Score = 0.1;
Check(!await verifier.VerifyAsync("test-token", "contact") && verifier.DevelopmentFailure!.Contains("0.10"), "local diagnostic identifies low score without exposing credentials");
handler.Score = 0.9;
Check(await verifier.VerifyAsync("test-token", "contact") && verifier.DevelopmentFailure is null, "successful retry clears diagnostic");
environment.EnvironmentName = "Production";
handler.Score = 0.5;
Check(await verifier.VerifyAsync("test-token", "contact"), "threshold boundary accepted");
config["Recaptcha:ScoreThreshold"] = "0.8";
Check(!await verifier.VerifyAsync("test-token", "contact"), "configured threshold applied");
config["Recaptcha:ScoreThreshold"] = "-1";
Check(!await verifier.VerifyAsync("test-token", "contact"), "invalid threshold fails closed");
config["Recaptcha:ScoreThreshold"] = "0.5"; handler.Score = 0.9;
handler.Errors = ["Over free quota."];
Check(!await verifier.VerifyAsync("test-token", "contact"), "provider errors rejected even with success and high score");
handler.Errors = [];
config["Recaptcha:UseRecaptchaNet"] = "true"; handler.ExpectedHost = "www.recaptcha.net";
Check(await verifier.VerifyAsync("test-token", "contact"), "alternate verification domain supported");
config["Recaptcha:UseRecaptchaNet"] = "false"; handler.ExpectedHost = "www.google.com";
handler.Success = false;
Check(!await verifier.VerifyAsync("test-token", "contact"), "Google rejects invalid or reused token");
handler.Success = true; handler.Host = "evil.test";
Check(!await verifier.VerifyAsync("test-token", "contact"), "wrong hostname blocked");
handler.Host = "test.vitalreachhub.com"; handler.Time = DateTimeOffset.UtcNow.AddMinutes(-3);
Check(!await verifier.VerifyAsync("test-token", "contact"), "expired challenge blocked");
handler.Time = DateTimeOffset.UtcNow.AddMinutes(2);
Check(!await verifier.VerifyAsync("test-token", "contact"), "future challenge blocked");
handler.Time = DateTimeOffset.UtcNow; handler.Malformed = true;
Check(!await verifier.VerifyAsync("test-token", "contact"), "malformed response blocked");
handler.Malformed = false; handler.Status = HttpStatusCode.ServiceUnavailable;
Check(!await verifier.VerifyAsync("test-token", "contact"), "provider HTTP failure blocked");
handler.Status = HttpStatusCode.OK; handler.Throw = true;
Check(!await verifier.VerifyAsync("test-token", "contact"), "provider network failure blocked");
handler.Throw = false;
config["Recaptcha:SecretKey"] = "";
Check(!await verifier.VerifyAsync("test-token", "contact"), "missing configuration blocked");
config["Recaptcha:DevelopmentBypass"] = "true";
Check(!await verifier.VerifyAsync(null, "contact"), "development bypass ignored in production");
environment.EnvironmentName = "Development";
Check(await verifier.VerifyAsync(null, "contact"), "explicit development bypass available");
environment.EnvironmentName = "Production";

// A forged contact submit without a challenge must fail before accessing the database.
var contact = new Contact();
const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
typeof(Contact).GetProperty("Captcha", flags)!.SetValue(contact, verifier);
var input = typeof(Contact).GetField("Input", flags)!.GetValue(contact)!;
input.GetType().GetProperty("Message")!.SetValue(input, "Keep this message for retry");
await (Task)typeof(Contact).GetMethod("SubmitAsync", flags)!.Invoke(contact, null)!;
Check((bool)typeof(Contact).GetField("IsError", flags)!.GetValue(contact)!, "contact rejects unverified request without database access");
Check(ReferenceEquals(input, typeof(Contact).GetField("Input", flags)!.GetValue(contact)), "contact input retained after verification failure");
using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options;
await using (var db = new CatalogDbContext(options))
{
    await db.Database.EnsureCreatedAsync();
    db.Products.AddRange(new ProductEntity { Slug="public", Name="Public product", IsPublished=true }, new ProductEntity { Slug="private", Name="Private product", IsPublished=false });
    db.Locations.AddRange(new Location { CenterName="Public location", Status=LocationStatus.Published }, new Location { CenterName="Private location", Status=LocationStatus.Draft });
    await db.SaveChangesAsync();
}
config["Chatbot:Enabled"] = "true"; config["Chatbot:ApiKey"] = "fake-api-key"; config["Chatbot:Model"] = "test-model";
var ai = new FakeAi();
var chat = new ChatService(verifier, new ClientFactory(ai), config, new DbFactory(options), new ChatBudget(config));
Check(await chat.SendAsync("Hello", null) is not null && ai.Calls == 0, "unverified chat cannot call AI");
Check(!await chat.StartAsync(null), "chat does not accept missing captcha in production");
config["Recaptcha:SecretKey"] = "test-secret";
handler.Action = "support_start";
Check(await chat.StartAsync("test-token"), "chat accepts server-verified captcha");
Check(await chat.SendAsync(new string('x',1001), null) is not null && ai.Calls == 0, "oversized chat blocked before AI");
handler.Action = "support_chat";
Check(await chat.SendAsync("Missing token", null) is not null && ai.Calls == 0, "started chat still requires fresh verification");
handler.Score = 0.1;
Check(await chat.SendAsync("Low score", "fresh-token") is not null && ai.Calls == 0, "low-score chat request cannot reach AI");
handler.Score = 0.9; handler.Action = "contact";
Check(await chat.SendAsync("Wrong action", "fresh-token") is not null && ai.Calls == 0, "contact token cannot authorize AI request");
handler.Action = "support_chat";
Check(await chat.SendAsync("Where can I visit?", "fresh-token") is null && ai.Calls == 1, "verified chat sends request");
Check(ai.Body.Contains("Public product") && ai.Body.Contains("Public location") && !ai.Body.Contains("Private product") && !ai.Body.Contains("Private location"), "only published site context is sent");
using (var payload = JsonDocument.Parse(ai.Body))
    Check(!payload.RootElement.GetProperty("store").GetBoolean() && payload.RootElement.GetProperty("max_output_tokens").GetInt32() == 2048, "AI storage disabled and response budget bounded");
Check(chat.Messages.Count == 2 && chat.Messages[1].Text == "Please visit our contact page.", "AI answer parsed into bounded history");
using (var payload = JsonDocument.Parse(ai.Body))
    Check(payload.RootElement.GetProperty("reasoning").GetProperty("effort").GetString() == "low", "reasoning effort leaves room for visible output");
Check(await chat.SendAsync("Again", "fresh-token") is not null && ai.Calls == 1, "rapid repeated requests blocked");
chat.End();
Check(chat.Messages.Count == 0 && !chat.IsVerified, "ending chat clears history and verification");
foreach (var scenario in new[]
{
    (Json: """{"status":"incomplete","incomplete_details":{"reason":"max_output_tokens"},"output":[{"type":"reasoning"}]}""", Error: "ran out of space"),
    (Json: """{"status":"incomplete","incomplete_details":{"reason":"max_output_tokens"},"output":[{"type":"message","content":[{"type":"output_text","text":"A truncated answer"}]}]}""", Error: "ran out of space"),
    (Json: """{"status":"completed","output":[{"type":"reasoning"}]}""", Error: "empty reply"),
    (Json: """{"status":"completed","output":[{"type":"message","content":[{"type":"refusal","refusal":"No"}]}]}""", Error: "can't help"),
    (Json: """{"status":"failed","output":[]}""", Error: "could not complete")
})
{
    ai.ResponseBody = scenario.Json;
    var attempt = new ChatService(verifier, new ClientFactory(ai), config, new DbFactory(options), new ChatBudget(config));
    handler.Action = "support_start";
    await attempt.StartAsync("start-token");
    handler.Action = "support_chat";
    var error = await attempt.SendAsync("Where is the shop?", "message-token");
    Check(error?.Contains(scenario.Error) == true && attempt.Messages.Count == 0, "unsuccessful AI response classified and excluded from history: " + scenario.Error);
}
config["Chatbot:MaxRequestsPerHour"] = "1";
var budget = new ChatBudget(config);
Check(budget.TryTake() && !budget.TryTake(), "shared hourly budget enforced");
Console.WriteLine($"PASS: {checks} reCAPTCHA and chatbot checks.");

sealed class FakeGoogle : HttpMessageHandler
{
    public int Calls;
    public string RequestBody = "", Action = "contact", ExpectedHost = "www.google.com"; public double? Score = 0.9; public string[] Errors = [];
    public bool Success = true, Malformed, Throw;
    public string Host = "test.vitalreachhub.com";
    public DateTimeOffset Time = DateTimeOffset.UtcNow;
    public HttpStatusCode Status = HttpStatusCode.OK;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        if (request.Method != HttpMethod.Post || request.RequestUri!.Host != ExpectedHost) throw new Exception("Unexpected verification endpoint");
        RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
        if (Throw) throw new HttpRequestException();
        return new HttpResponseMessage(Status) { Content = new StringContent(Malformed ? "not JSON" : JsonSerializer.Serialize(new Dictionary<string, object?> { ["success"]=Success, ["hostname"]=Host, ["challenge_ts"]=Time, ["action"]=Action, ["score"]=Score, ["error-codes"]=Errors })) };
    }
}
sealed class EnvironmentStub : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "Checks";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
sealed class DbFactory(DbContextOptions<CatalogDbContext> options) : IDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext() => new(options);
}
sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler:false);
}
sealed class FakeAi : HttpMessageHandler
{
    public int Calls;
    public string Body = ""; public string ResponseBody = """{"status":"completed","output":[{"type":"message","content":[{"type":"output_text","text":"Please visit our contact page."}]}]}""";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        if (request.RequestUri!.ToString() != "https://api.openai.com/v1/responses") throw new Exception("Wrong AI endpoint");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ResponseBody) };
    }
}

