using Carrier.Domain;

namespace Carrier.Application;

public sealed record CarrierSearch(
    string? Origin = null, string? Destination = null, Equipment? Equipment = null,
    DateOnly? PickupDate = null, int WeightLb = 1, decimal? MaxRatePerMile = null,
    string? Query = null, int Page = 1, int PageSize = 6, string Sort = "recommended")
{
    public CarrierSearch Normalize() => this with
    {
        Origin = Origin?.Trim().ToUpperInvariant(), Destination = Destination?.Trim().ToUpperInvariant(),
        Query = Query?.Trim(), Sort = Sort?.Trim().ToLowerInvariant() ?? ""
    };

    public Dictionary<string, string[]> Validate(DateOnly today, bool shipmentRequired = false)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string key, string message) => errors[key] = [message];
        if (Page < 1 || Page > 10_000) Add(nameof(Page), "Page must be between 1 and 10000.");
        if (PageSize < 1 || PageSize > 50) Add(nameof(PageSize), "Page size must be between 1 and 50.");
        if (WeightLb < 1 || WeightLb > 100_000) Add(nameof(WeightLb), "Weight must be between 1 and 100000 lb.");
        if (MaxRatePerMile is <= 0 or > 100) Add(nameof(MaxRatePerMile), "Rate must be greater than 0 and at most 100 USD/mile.");
        if (Equipment.HasValue && !Enum.IsDefined(Equipment.Value)) Add(nameof(Equipment), "Unknown equipment type.");
        if (Sort is not ("recommended" or "rate" or "name")) Add(nameof(Sort), "Sort must be recommended, rate, or name.");
        if (Query?.Length > 100) Add(nameof(Query), "Search text must be at most 100 characters.");
        if (Origin?.Length > 80) Add(nameof(Origin), "Origin must be at most 80 characters.");
        if (Destination?.Length > 80) Add(nameof(Destination), "Destination must be at most 80 characters.");
        if (!string.IsNullOrEmpty(Origin) && Origin == Destination) Add(nameof(Destination), "Choose different cities.");
        if (PickupDate < today) Add(nameof(PickupDate), "Pickup cannot be in the past (UTC).");
        if (PickupDate > today.AddDays(365)) Add(nameof(PickupDate), "Pickup must be within one year.");
        if (shipmentRequired)
        {
            if (string.IsNullOrWhiteSpace(Origin)) Add(nameof(Origin), "Origin is required.");
            if (string.IsNullOrWhiteSpace(Destination)) Add(nameof(Destination), "Destination is required.");
            if (!Equipment.HasValue) Add(nameof(Equipment), "Equipment is required.");
            if (!PickupDate.HasValue) Add(nameof(PickupDate), "Pickup date is required.");
        }
        return errors;
    }
}

public sealed record CarrierCard(Guid Id, string Name, string DotNumber, Equipment Equipment,
    int CapacityLb, decimal RatePerMile, double OnTimeRate, double SafetyScore, int CompletedLoads,
    DateOnly AvailableFrom, DateOnly AvailableThrough)
{
    public ScoreBreakdown Score => RankingPolicy.Explain(OnTimeRate, SafetyScore, CompletedLoads);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize,
    string RankingVersion)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public interface ICarrierReadRepository
{
    Task<PagedResult<CarrierCard>> SearchAsync(CarrierSearch search, bool rankingEnabled, CancellationToken ct);
    Task<CarrierCard?> FindAsync(Guid id, CancellationToken ct);
}

public sealed class SearchValidationException(Dictionary<string, string[]> errors) : Exception("Invalid search.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}

public sealed class CarrierDiscovery(ICarrierReadRepository repository, TimeProvider clock)
{
    public Task<PagedResult<CarrierCard>> ExecuteAsync(CarrierSearch search, bool shipmentRequired,
        bool rankingEnabled, CancellationToken ct)
    {
        search = search.Normalize();
        var errors = search.Validate(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), shipmentRequired);
        if (errors.Count > 0) throw new SearchValidationException(errors);
        return repository.SearchAsync(search, rankingEnabled, ct);
    }

    public Task<CarrierCard?> FindAsync(Guid id, CancellationToken ct) => repository.FindAsync(id, ct);
}
