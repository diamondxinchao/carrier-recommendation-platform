using Carrier.Application;
using Carrier.Domain;
using Carrier.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Carrier.Tests;

public sealed class RepositoryTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);
    private static CarrierProfile Make(int index, double reliability = .95, Equipment equipment = Equipment.DryVan,
        int capacity = 44000, decimal rate = 2.5m, DateOnly? available = null) =>
        new(Guid.Parse($"00000000-0000-0000-0000-{index:D12}"), $"Carrier {index}", $"DEMO{index}", equipment,
            capacity, rate, reliability, 90, 500, available ?? Today, Today.AddDays(90));

    [Fact]
    public async Task Eligibility_excludes_wrong_lane_equipment_capacity_date_and_rate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new CarrierDbContext(new DbContextOptionsBuilder<CarrierDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var eligible = Make(1); eligible.AddLane("Chicago, IL", "Dallas, TX");
        var wrongLane = Make(2); wrongLane.AddLane("Chicago, IL", "Seattle, WA"); wrongLane.AddLane("Atlanta, GA", "Dallas, TX");
        var equipment = Make(3, equipment: Equipment.Flatbed); equipment.AddLane("Chicago, IL", "Dallas, TX");
        var capacity = Make(4, capacity: 100); capacity.AddLane("Chicago, IL", "Dallas, TX");
        var date = Make(5, available: Today.AddDays(1)); date.AddLane("Chicago, IL", "Dallas, TX");
        var rate = Make(6, rate: 9); rate.AddLane("Chicago, IL", "Dallas, TX");
        db.AddRange(eligible, wrongLane, equipment, capacity, date, rate); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var results = await new EfCarrierReadRepository(db).SearchAsync(new CarrierSearch(
            "Chicago, IL", "Dallas, TX", Equipment.DryVan, Today, 20000, 3).Normalize(), true, default);
        Assert.Equal(eligible.Id, Assert.Single(results.Items).Id);
        Assert.Equal(1, results.TotalCount);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Ranking_covers_all_candidates_before_pagination_and_ties_are_stable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new CarrierDbContext(new DbContextOptionsBuilder<CarrierDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.AddRange(Make(1, .6), Make(2, .8), Make(3, .99), Make(4, .99)); await db.SaveChangesAsync();
        var repository = new EfCarrierReadRepository(db);
        var first = await repository.SearchAsync(new(PageSize: 1), true, default);
        var second = await repository.SearchAsync(new(Page: 2, PageSize: 1), true, default);
        Assert.Equal(4, first.TotalCount); Assert.Equal(4, first.TotalPages);
        Assert.Equal("Carrier 3", Assert.Single(first.Items).Name);
        Assert.Equal("Carrier 4", Assert.Single(second.Items).Name);
        var fallback = await repository.SearchAsync(new(PageSize: 1), false, default);
        Assert.Equal("Carrier 1", Assert.Single(fallback.Items).Name); Assert.Equal("unranked", fallback.RankingVersion);
    }

    [Fact]
    public async Task Availability_is_inclusive_and_out_of_range_page_is_empty()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new CarrierDbContext(new DbContextOptionsBuilder<CarrierDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(); db.Add(Make(1)); await db.SaveChangesAsync();
        var repository = new EfCarrierReadRepository(db);
        Assert.Single((await repository.SearchAsync(new(PickupDate: Today.AddDays(90)), true, default)).Items);
        Assert.Empty((await repository.SearchAsync(new(PickupDate: Today.AddDays(91)), true, default)).Items);
        var page = await repository.SearchAsync(new(Page: 2, PageSize: 1), true, default);
        Assert.Empty(page.Items); Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task SqlServer_translates_filter_ranking_and_pagination()
    {
        var connection = Environment.GetEnvironmentVariable("CARRIER_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Set CARRIER_TEST_SQLSERVER to a disposable SQL Server database. Or exclude Category=SqlServer.");
        await using var db = new CarrierDbContext(new DbContextOptionsBuilder<CarrierDbContext>().UseSqlServer(connection).Options);
        await db.Database.MigrateAsync();
        await DemoData.SeedAsync(db, Today);
        var search = new CarrierSearch("Chicago, IL", "Dallas, TX", Equipment.DryVan, Today, 20000, 3, PageSize: 2).Normalize();
        var result = await new EfCarrierReadRepository(db).SearchAsync(search, true, default);
        Assert.NotEmpty(result.Items); Assert.True(result.Items.Count <= 2);
        Assert.All(result.Items, c => { Assert.Equal(Equipment.DryVan, c.Equipment); Assert.True(c.RatePerMile <= 3); });
        Assert.Equal(result.Items.OrderByDescending(c => c.Score.Total).Select(c => c.Id), result.Items.Select(c => c.Id));
    }
}
