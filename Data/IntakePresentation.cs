namespace VitalReach.Web.Data;

public static class IntakePresentation
{
    public const string ClearedStatus = "Reviewed — no outstanding action";
    public static readonly string[] ReviewStatuses = ["In review", "Reviewed", ClearedStatus];
    public static string Color(int priority) => priority switch { 3 => "red", 2 => "orange", 1 => "yellow", _ => "blue" };
    public static string Icon(int priority) => priority switch { 3 => "!", 2 => "▲", 1 => "△", _ => "i" };
    public static string StatusColor(string status) => status == ClearedStatus ? "green" : "blue";
}
