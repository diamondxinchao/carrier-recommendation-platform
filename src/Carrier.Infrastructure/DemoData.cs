using Carrier.Domain;
using Microsoft.EntityFrameworkCore;

namespace Carrier.Infrastructure;

public static class DemoData
{
    public static readonly string[] Cities = ["Chicago, IL", "Dallas, TX", "Atlanta, GA", "Los Angeles, CA", "Seattle, WA", "Denver, CO"];
    public static async Task SeedAsync(CarrierDbContext db, DateOnly today, CancellationToken ct = default)
    {
        if (await db.Carriers.AnyAsync(ct)) return;
        string[] names = ["Northstar Freight", "Blue Ridge Logistics", "Atlas Transport", "Summit Carriers", "Evergreen Express",
            "Prairie Line", "Pacific Haul", "Ironwood Freight", "Cedar Logistics", "Horizon Transport",
            "Red Oak Carriers", "Clearwater Express", "Silverline Freight", "Waypoint Logistics", "Highland Transport",
            "Meridian Carriers", "Westward Express", "Stonebridge Freight", "Pioneer Logistics", "Coastline Transport",
            "Arrowhead Carriers", "Crossroads Freight", "Lakefront Logistics", "Ridgeline Transport"];
        for (var i = 0; i < names.Length; i++)
        {
            var c = new CarrierProfile(Guid.Parse($"00000000-0000-0000-0000-{i + 1:D12}"), names[i],
                $"DEMO{i + 1000}", (Equipment)(i % 3), 42000 + i % 4 * 2000, 1.85m + i % 8 * .15m,
                .88 + i % 12 * .01, 82 + i % 18, 100 + i * 43, today.AddDays(i % 7 == 6 ? 7 : -1), today.AddDays(90));
            for (var o = 0; o < Cities.Length; o++)
                for (var d = 0; d < Cities.Length; d++)
                    if (o != d && (i % 4 == 0 || (i + o + d) % 3 != 0)) c.AddLane(Cities[o], Cities[d]);
            db.Carriers.Add(c);
        }
        await db.SaveChangesAsync(ct);
    }
}
