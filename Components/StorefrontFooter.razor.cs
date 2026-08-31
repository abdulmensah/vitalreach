#nullable enable
using Microsoft.AspNetCore.Components;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components;

public partial class StorefrontFooter
{
    [Parameter] public HeadquartersSettings? Headquarters { get; set; }
}
