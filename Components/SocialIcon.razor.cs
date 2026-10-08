#nullable enable
using Microsoft.AspNetCore.Components;
using VitalReach.Web.Data;
namespace VitalReach.Web.Components;
public partial class SocialIcon
{
    [Parameter] public SocialPlatform Platform { get; set; }
}
