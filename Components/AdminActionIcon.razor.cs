#nullable enable
using Microsoft.AspNetCore.Components;

namespace VitalReach.Web.Components;

public partial class AdminActionIcon
{
    [Parameter] public string Name { get; set; } = "edit";
    private string Path => Name switch
    {
        "add" => "M12 5v14M5 12h14",
        "save" => "M5 3h12l4 4v14H3V3h2Zm2 0v6h10V3M7 21v-8h10v8",
        "delete" => "M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7M14 10v7",
        "close" => "m6 6 12 12M6 18 18 6",
        "refresh" => "M20 7v5h-5M4 17v-5h5M5.5 7a8 8 0 0 1 13-1L20 9M4 15l1.5 3a8 8 0 0 0 13-1",
        "up" => "M12 20V4m-7 7 7-7 7 7",
        "down" => "M12 4v16m-7-7 7 7 7-7",
        "left" => "M20 12H4m7-7-7 7 7 7",
        "right" => "M4 12h16m-7-7 7 7-7 7",
        "read" => "M3 9l9-6 9 6v12H3V9Zm0 0 9 7 9-7",
        "unread" => "M3 5h18v14H3V5Zm0 0 9 8 9-8",
        "enable" => "m5 12 4 4L19 6",
        "disable" => "M4 4l16 16M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20",
        "clinical" => "M9 3h6v6h6v6h-6v6H9v-6H3V9h6V3Z",
        "clinical-off" => "M9 3h6v6h6v6h-6v6H9v-6H3V9h6V3ZM2 2l20 20",
        "shield" => "m12 2 9 4v6c0 5-9 10-9 10S3 17 3 12V6l9-4Zm-4 10 3 3 5-6",
        "shield-off" => "m12 2 9 4v6c0 5-9 10-9 10S3 17 3 12V6l9-4ZM2 2l20 20",
        "review" => "M8 3h8v4H8V3Zm0 2H4v16h16V5h-4M8 12h8M8 16h5",
        "tax" => "M5 2h14v20H5V2Zm3 3h8v4H8V5Zm0 8h1m6 0h1m-8 4h1m6 0h1",
        "quote" => "M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6m-6 4h6",
        "ship" => "M2 5h13v13H2V5Zm13 5h4l3 4v4h-7M5 18a2 2 0 1 0 4 0m8 0a2 2 0 1 0 4 0",
        "payment" => "M2 5h20v14H2V5Zm0 5h20M6 15h4",
        "upload" => "M12 16V3m-5 5 5-5 5 5M3 16v5h18v-5",
        _ => "m16 3 5 5-12 12-6 1 1-6L16 3Zm-2 2 5 5"
    };
}
