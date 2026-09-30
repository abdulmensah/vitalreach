using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace VitalReach.Web.Data;

/// <summary>Process-local abuse protection. A reverse proxy should provide shared limits for multi-instance hosting.</summary>
public sealed class IntakeSubmissionLimiter : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 10000 });
    private readonly object gate = new();
    public bool TryAcquire(string address)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address)));
        lock (gate)
        {
            if (!cache.TryGetValue(key, out Counter? counter))
            {
                counter = new();
                cache.Set(key, counter, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1), Size = 1 });
            }
            return ++counter!.Count <= 20;
        }
    }
    private sealed class Counter { public int Count; }
    public void Dispose() => cache.Dispose();
}
