using System.Reflection;
using Microsoft.JSInterop;
using VitalReach.Web.Components.Pages;
using VitalReach.Web.Data;

int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
var runtime = new FakeRuntime();
var confirmation = new AdminConfirmation(runtime);
int writes = 0;
runtime.Response = Task.FromResult(false);
await confirmation.RunAsync("Delete?", () => writes++);
Check(writes == 0, "Cancel executes no action");
runtime.Response = Task.FromResult(true);
await confirmation.RunAsync("Save?", () => writes++);
Check(writes == 1, "OK executes one action");
var pending = new TaskCompletionSource<bool>(); runtime.Response = pending.Task;
var first = confirmation.RunAsync("Save once?", () => writes++);
await confirmation.RunAsync("Duplicate?", () => writes++);
pending.SetResult(true); await first;
Check(writes == 2, "double click while dialog open ignored");
runtime.Response = Task.FromResult(true);
var work = new TaskCompletionSource();
first = confirmation.RunAsync("Saving?", () => work.Task);
await confirmation.RunAsync("Duplicate during save?", () => writes++);
work.SetResult(); await first;
Check(writes == 2, "double click during write ignored");
runtime.Response = Task.FromException<bool>(new JSDisconnectedException("Disconnected"));
await confirmation.RunAsync("Disconnected?", () => writes++);
Check(writes == 2, "disconnected confirmation fails closed");
runtime.Response = Task.FromResult(true);
try { await confirmation.RunAsync("Failure?", (Func<Task>)(() => throw new InvalidOperationException())); } catch (InvalidOperationException) { }
await confirmation.RunAsync("Recovered?", () => writes++);
Check(writes == 3, "gate releases after failed operation");

// Every mutating button handler must ask before accessing any dependencies or changing its draft.
runtime.Response = Task.FromResult(false);
const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
async Task Reject(object page, string method, params object[] arguments)
{
    page.GetType().GetProperty("Confirmation", flags)!.SetValue(page, confirmation);
    int before = runtime.Calls;
    await (Task)page.GetType().GetMethod(method, flags)!.Invoke(page, arguments)!;
    Check(runtime.Calls == before + 1, page.GetType().Name + "." + method + " asks confirmation");
}
var product = new AdminProducts();
await Reject(product, "Save"); await Reject(product, "Delete"); await Reject(product, "AddVariant");
await Reject(product, "RemoveVariant", new ProductVariant { Sku = "TEST" });
await Reject(product, "RemoveImage"); await Reject(product, "AddGalleryImage");
await Reject(product, "MoveProduct", new ProductEntity { Name = "Test" }, 1);
await Reject(product, "MoveGalleryImage", new ProductImage(), -1);
await Reject(product, "RemoveGalleryImage", new ProductImage());
var user = AdminUser.Create("test@example.test", "Test", "test");
await Reject(new AdminUsers(), "AddUser"); await Reject(new AdminUsers(), "Toggle", user);
await Reject(new AdminUsers(), "Delete", user);
await Reject(new AdminUsers(), "ToggleRole", user, true);
await Reject(new AdminUsers(), "ToggleRole", user, false);
await Reject(new AdminHeadquarters(), "SaveAsync");
await Reject(new AdminHeadquarters(), "NewAsync");
await Reject(new AdminHeadquarters(), "EditAsync", new Location());
await Reject(new AdminSocial(), "SaveAsync");
await Reject(new AdminSocial(), "NewAsync");
await Reject(new AdminSocial(), "EditAsync", new SocialLink());
await Reject(new AdminConsultations(), "SaveAsync");
await Reject(new AdminContacts(), "ToggleReadAsync", new ContactSubmission());
await Reject(new AdminContacts(), "DeleteAsync", new ContactSubmission());
await Reject(new AdminOrders(), "CalculateTax");
foreach (var action in new[] { "quote", "paid", "ship", "cancel" }) await Reject(new AdminOrders(), "Act", action);
Check(runtime.LastMessage.Contains("Cancel unpaid order"), "specific action described");
Console.WriteLine($"PASS: {checks} admin confirmation checks.");

sealed class FakeRuntime : IJSRuntime
{
    public Task<bool> Response = Task.FromResult(false);
    public int Calls;
    public string LastMessage = "";
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier != "adminConfirm.ask") throw new Exception("Unexpected JS call");
        Calls++; LastMessage = (string)args![0]!;
        return (TValue)(object)await Response;
    }
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
}
