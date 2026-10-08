using System.Text.Json.Serialization;

namespace VitalReach.Web.Data;

public sealed class RecaptchaService(HttpClient client, IConfiguration configuration, IHostEnvironment environment, ILogger<RecaptchaService>? logger = null)
{
    private string? FailureReason;
    public string? DevelopmentFailure => environment.IsDevelopment() ? FailureReason : null;
    private bool Reject(string reason)
    {
        FailureReason = reason;
        logger?.LogWarning("reCAPTCHA verification rejected: {Reason}", reason);
        return false;
    }
    public bool DevelopmentBypass => environment.IsDevelopment() && configuration.GetValue<bool>("Recaptcha:DevelopmentBypass");
    public string SiteKey => configuration["Recaptcha:SiteKey"]?.Trim() ?? "";
    private string SecretKey => configuration["Recaptcha:SecretKey"]?.Trim() ?? "";
    public bool UseRecaptchaNet => configuration.GetValue<bool>("Recaptcha:UseRecaptchaNet");
    private double ScoreThreshold => configuration.GetValue<double?>("Recaptcha:ScoreThreshold") ?? 0.5;
    private string[] Hosts => (configuration["Recaptcha:AllowedHosts"] ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    public bool IsConfigured => SiteKey.Length > 0 && SecretKey.Length > 0 && Hosts.Length > 0;

    public async Task<bool> VerifyAsync(string? token, string expectedAction, CancellationToken cancellationToken = default)
    {
        FailureReason = null;
        if (DevelopmentBypass) return true;
        if (!IsConfigured) return Reject("Required reCAPTCHA configuration is missing.");
        if (string.IsNullOrWhiteSpace(token)) return Reject("The browser did not return a reCAPTCHA token.");
        if (token.Length > 8192 || string.IsNullOrWhiteSpace(expectedAction)) return Reject("The verification request is invalid.");
        var threshold = ScoreThreshold;
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1) return Reject("ScoreThreshold must be between 0 and 1.");
        try
        {
            using var body = new FormUrlEncodedContent(new Dictionary<string, string> { ["secret"] = SecretKey, ["response"] = token });
            var authority = UseRecaptchaNet ? "https://www.recaptcha.net" : "https://www.google.com";
            using var response = await client.PostAsync($"{authority}/recaptcha/api/siteverify", body, cancellationToken);
            if (!response.IsSuccessStatusCode) return Reject($"Google verification returned HTTP {(int)response.StatusCode}.");
            var result = await response.Content.ReadFromJsonAsync<Verification>(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            if (result is null) return Reject("Google returned an empty verification result.");
            if (result.ErrorCodes?.Contains("invalid-input-secret") == true) return Reject("Google rejected the secret key; check the registered key pair.");
            if (result.ErrorCodes?.Contains("timeout-or-duplicate") == true) return Reject("The token expired or was already used.");
            if (!result.Success || result.ErrorCodes is not { Length: 0 }) return Reject("Google rejected the token or reported a provider error; check the key type and matching key pair.");
            if (!Hosts.Contains(result.Hostname, StringComparer.OrdinalIgnoreCase)) return Reject("The verified hostname is not in Recaptcha:AllowedHosts.");
            if (!string.Equals(result.Action, expectedAction, StringComparison.Ordinal)) return Reject("The token action does not match this request; a v3 score-based key is required.");
            if (result.Score is not double score || !double.IsFinite(score) || score is < 0 or > 1) return Reject("Google did not return a valid v3 score; check the registered key type.");
            if (score < threshold) return Reject(FormattableString.Invariant($"Google returned score {score:0.00}, below the configured threshold {threshold:0.00}."));
            if (result.ChallengeTime < now.AddMinutes(-2) || result.ChallengeTime > now.AddSeconds(30)) return Reject("The token timestamp is outside the allowed window; check the server clock.");
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or OperationCanceledException)
        { return Reject(ex is OperationCanceledException ? "Google verification timed out." : ex is HttpRequestException ? "The server could not reach Google verification." : "Google returned an unreadable verification response."); }
    }

    private sealed class Verification
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("hostname")] public string Hostname { get; set; } = "";
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("score")] public double? Score { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; } = [];
        [JsonPropertyName("challenge_ts")] public DateTimeOffset ChallengeTime { get; set; }
    }
}
