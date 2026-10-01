using Microsoft.AspNetCore.Components.Server.Circuits;

namespace VitalReach.Web.Data;

public sealed class ConnectionDiagnostics(ILogger<ConnectionDiagnostics> logger) : CircuitHandler
{
    // Independent diagnostic ID, never a circuit reconnect token, user ID, URL, or form data.
    private readonly string diagnosticId = Guid.NewGuid().ToString("N")[..12];
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Browser connection lost ({DiagnosticId}); awaiting reconnect", diagnosticId);
        return Task.CompletedTask;
    }
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Browser connection active ({DiagnosticId})", diagnosticId);
        return Task.CompletedTask;
    }
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Browser session released ({DiagnosticId})", diagnosticId);
        return Task.CompletedTask;
    }
}
