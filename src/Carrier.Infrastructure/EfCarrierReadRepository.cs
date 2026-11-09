using System.Linq.Expressions;
using Carrier.Application;
using Carrier.Domain;
using Microsoft.EntityFrameworkCore;

namespace Carrier.Infrastructure;

public sealed class EfCarrierReadRepository(CarrierDbContext db) : ICarrierReadRepository
{
    private static readonly Expression<Func<CarrierProfile, CarrierCard>> Projection = c => new(
        c.Id, c.Name, c.DotNumber, c.Equipment, c.CapacityLb, c.RatePerMile, c.OnTimeRate,
        c.SafetyScore, c.CompletedLoads, c.AvailableFrom, c.AvailableThrough);

    public async Task<PagedResult<CarrierCard>> SearchAsync(CarrierSearch search, bool rankingEnabled, CancellationToken ct)
    {
        var query = db.Carriers.AsNoTracking().Where(c => c.CapacityLb >= search.WeightLb);
        // A single Any keeps origin/destination on the same service lane.
        if (!string.IsNullOrEmpty(search.Origin) || !string.IsNullOrEmpty(search.Destination))
            query = query.Where(c => c.Lanes.Any(l =>
                (string.IsNullOrEmpty(search.Origin) || l.Origin == search.Origin) &&
                (string.IsNullOrEmpty(search.Destination) || l.Destination == search.Destination)));
        if (search.Equipment.HasValue) query = query.Where(c => c.Equipment == search.Equipment);
        if (search.PickupDate.HasValue)
            query = query.Where(c => c.AvailableFrom <= search.PickupDate && c.AvailableThrough >= search.PickupDate);
        if (search.MaxRatePerMile.HasValue) query = query.Where(c => c.RatePerMile <= search.MaxRatePerMile);
        if (!string.IsNullOrEmpty(search.Query))
        {
            var term = search.Query.ToUpperInvariant();
            query = query.Where(c => c.Name.ToUpper().Contains(term) || c.DotNumber.Contains(term));
        }

        var count = await query.CountAsync(ct);
        var ordered = search.Sort switch
        {
            "rate" => query.OrderBy(c => (double)c.RatePerMile).ThenBy(c => c.Id),
            "recommended" when rankingEnabled => query.OrderByDescending(RankingPolicy.ScoreExpression).ThenBy(c => c.Id),
            _ => query.OrderBy(c => c.Name).ThenBy(c => c.Id)
        };
        var items = await ordered.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(Projection).ToListAsync(ct);
        return new(items, count, search.Page, search.PageSize,
            search.Sort == "recommended" && rankingEnabled ? RankingPolicy.Version : "unranked");
    }

    public Task<CarrierCard?> FindAsync(Guid id, CancellationToken ct) =>
        db.Carriers.AsNoTracking().Where(c => c.Id == id).Select(Projection).SingleOrDefaultAsync(ct);
}
