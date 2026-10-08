using Microsoft.AspNetCore.DataProtection;

namespace VitalReach.Web.Data;

public sealed class CartSession(IHttpContextAccessor accessor, IDataProtectionProvider protection)
{
    public const string CookieName = "VitalReach.Cart";
    public string Id { get; } = Resolve(accessor.HttpContext, protection);

    public static string Resolve(HttpContext? context, IDataProtectionProvider protection)
    {
        if (context?.Items[CookieName] is string id) return id;
        if (context?.Request.Cookies[CookieName] is string cookie)
        {
            try
            {
                var value = protection.CreateProtector(CookieName).Unprotect(cookie);
                if (Guid.TryParseExact(value, "N", out _)) return value;
            }
            catch (System.Security.Cryptography.CryptographicException) { }
        }
        return "";
    }
}
