using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Carrier.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string database = Path.Combine(Path.GetTempPath(), $"carrier-test-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite", ["Database:Initialize"] = "true",
            ["ConnectionStrings:Carriers"] = $"Data Source={database};Pooling=False"
        }));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(database)) File.Delete(database);
    }
}

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Valid_shipment_returns_explainable_ranked_carriers()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/recommendations", new
        {
            origin = "Chicago, IL", destination = "Dallas, TX", equipment = "dryVan",
            pickupDate = DateTime.UtcNow.ToString("yyyy-MM-dd"), weightLb = 20000
        });
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("quality-v1", page.GetProperty("rankingVersion").GetString());
        Assert.True(page.GetProperty("items").GetArrayLength() > 0);
        Assert.True(page.GetProperty("items")[0].GetProperty("score").GetProperty("total").GetDouble() <= 100);
        Assert.True(response.Headers.Contains("Server-Timing"));
    }

    [Fact]
    public async Task Missing_shipment_fields_returns_validation_problem()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/recommendations", new { weightLb = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("WeightLb", out _));
    }

    [Theory]
    [InlineData("/api/carriers?pageSize=51")]
    [InlineData("/api/carriers?sort=unknown")]
    [InlineData("/api/carriers?equipment=999")]
    public async Task Invalid_query_returns_400(string url)
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Malformed_equipment_and_missing_carrier_are_handled()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/recommendations", new { equipment = "spaceship" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/carriers/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }
}
