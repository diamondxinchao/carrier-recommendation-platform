using System.Xml.Linq;
using Carrier.Application;
using Carrier.Domain;

var checks = new List<(string Name, Action Run)>();
var today = new DateOnly(2026, 11, 9);
CarrierProfile Make(double reliability = .95, double safety = 90, int loads = 250) => new(
    Guid.NewGuid(), "Test Carrier", "DEMO100", Equipment.DryVan, 44000, 2.5m,
    reliability, safety, loads, today, today.AddDays(90));
void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; received {actual}."); }
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
checks.Add(("Ranking: perfect history scores 100", () => Equal(100d, RankingPolicy.Explain(1, 100, 500).Total)));
checks.Add(("Ranking: no history scores zero", () => Equal(0d, RankingPolicy.Explain(0, 0, 0).Total)));
checks.Add(("Ranking: experience is capped", () => Equal(RankingPolicy.Explain(.95, 90, 500), RankingPolicy.Explain(.95, 90, 9000))));
checks.Add(("Ranking: SQL expression agrees with the explanation across 100 scenarios", () =>
{
    var score = RankingPolicy.ScoreExpression.Compile();
    for (var i = 0; i < 100; i++)
    {
        var c = Make(i / 100d, i, i * 17);
        Equal(RankingPolicy.Explain(c.OnTimeRate, c.SafetyScore, c.CompletedLoads).Total, Math.Round(score(c), 2));
    }
}));
checks.Add(("Domain: invalid performance data is rejected", () =>
{
    Throws<ArgumentOutOfRangeException>(() => Make(double.NaN));
    Throws<ArgumentOutOfRangeException>(() => Make(1.1));
    Throws<ArgumentOutOfRangeException>(() => Make(.9, -1));
    Throws<ArgumentOutOfRangeException>(() => Make(.9, 50, -1));
}));
checks.Add(("Domain: lanes normalize and deduplicate", () =>
{
    var c = Make(); c.AddLane(" Chicago, IL ", "Dallas, TX"); c.AddLane("CHICAGO, IL", "DALLAS, TX");
    Equal(1, c.Lanes.Count); Equal("CHICAGO, IL", c.Lanes[0].Origin);
    Throws<ArgumentException>(() => c.AddLane("Dallas, TX", "dallas, tx"));
}));
checks.Add(("Validation: recommendation requires shipment context", () => Equal(4, new CarrierSearch().Validate(today, true).Count)));
checks.Add(("Validation: capacity, pagination, date, and unknown sort are bounded", () =>
{
    var errors = new CarrierSearch(PickupDate: today.AddDays(-1), WeightLb: 0, Page: 0, PageSize: 51, Sort: "unsafe").Validate(today);
    Equal(5, errors.Count);
}));
checks.Add(("Validation: null sort becomes a validation error", () =>
    Equal(true, new CarrierSearch(Sort: null!).Normalize().Validate(today).ContainsKey("Sort"))));
checks.Add(("Validation: pickup today and exact bounds are accepted", () =>
    Equal(0, new CarrierSearch("Chicago, IL", "Dallas, TX", Equipment.DryVan, today, 100000, 100, Page: 10000, PageSize: 50).Normalize().Validate(today, true).Count)));
checks.Add(("Application: invalid requests never reach the repository", () =>
{
    var repository = new RecordingRepository();
    var discovery = new CarrierDiscovery(repository, new FixedClock(today));
    Throws<SearchValidationException>(() => discovery.ExecuteAsync(new(), true, true, default).GetAwaiter().GetResult());
    Equal(0, repository.Calls);
}));
checks.Add(("Application: valid requests normalize values and forward the feature flag", () =>
{
    var repository = new RecordingRepository();
    var discovery = new CarrierDiscovery(repository, new FixedClock(today));
    discovery.ExecuteAsync(new(" Chicago, IL ", "Dallas, TX", Equipment.DryVan, today), true, false, default).GetAwaiter().GetResult();
    Equal("CHICAGO, IL", repository.Search!.Origin); Equal(false, repository.RankingEnabled); Equal(1, repository.Calls);
}));
checks.Add(("Architecture: inward-only references and dependency-free domain", () =>
{
    var root = new DirectoryInfo(AppContext.BaseDirectory);
    while (root is not null && !File.Exists(Path.Combine(root.FullName, "CarrierRecommendation.sln"))) root = root.Parent;
    if (root is null) throw new Exception("Solution root not found.");
    foreach (var (project, allowed) in new[] { ("Domain", Array.Empty<string>()), ("Application", new[] { "Carrier.Domain.csproj" }), ("Infrastructure", new[] { "Carrier.Application.csproj" }), ("Api", new[] { "Carrier.Application.csproj", "Carrier.Infrastructure.csproj" }) })
    {
        var xml = XDocument.Load(Path.Combine(root.FullName, "src", $"Carrier.{project}", $"Carrier.{project}.csproj"));
        foreach (var reference in xml.Descendants("ProjectReference"))
            Equal(true, allowed.Contains(Path.GetFileName(reference.Attribute("Include")!.Value)));
        if (project is "Domain" or "Application") Equal(0, xml.Descendants("PackageReference").Count());
    }
}));

var failures = 0;
foreach (var (name, run) in checks)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {name}: {e.Message}"); }
}
Console.WriteLine($"{checks.Count - failures}/{checks.Count} core checks passed.");
return failures == 0 ? 0 : 1;

sealed class FixedClock(DateOnly date) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
sealed class RecordingRepository : ICarrierReadRepository
{
    public int Calls { get; private set; }
    public CarrierSearch? Search { get; private set; }
    public bool RankingEnabled { get; private set; }
    public Task<PagedResult<CarrierCard>> SearchAsync(CarrierSearch search, bool rankingEnabled, CancellationToken ct)
    {
        Calls++; Search = search; RankingEnabled = rankingEnabled;
        return Task.FromResult(new PagedResult<CarrierCard>([], 0, search.Page, search.PageSize, "test"));
    }
    public Task<CarrierCard?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult<CarrierCard?>(null);
}
