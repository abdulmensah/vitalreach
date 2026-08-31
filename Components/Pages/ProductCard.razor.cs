#nullable enable
using Microsoft.AspNetCore.Components;
using VitalReach.Web.Data;

namespace VitalReach.Web.Components.Pages;

public partial class ProductCard
{
    [Parameter, EditorRequired] public ProductEntity Product { get; set; } = default!;
    [Parameter] public EventCallback<ProductEntity> OnAdd { get; set; }
}
