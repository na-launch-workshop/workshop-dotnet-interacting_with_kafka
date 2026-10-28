using System.Net;
using System.Text.Json;
using HomeGrid;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HomeGrid.Tests;

/// <summary>Hosts the app without the Kafka consumer, so tests never touch a broker.</summary>
public sealed class HomeGridFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            var consumer = services.Single(d => d.ImplementationType == typeof(MeterReadingConsumer));
            services.Remove(consumer);
        });
}

public sealed class AppTests(HomeGridFactory factory) : IClassFixture<HomeGridFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly EventBuffer _events = factory.Services.GetRequiredService<EventBuffer>();

    [Fact]
    public async Task DashboardReturnsHtml()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("HomeGrid", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EventsEndpointEmpty()
    {
        _events.Clear();

        var response = await _client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EventsEndpointReturnsData()
    {
        _events.Clear();
        _events.Add(JsonSerializer.Deserialize<JsonElement>("""
            {
              "circuit_id": "CB-07",
              "room": "Kitchen",
              "type": "kitchen",
              "kwh": 1.5,
              "timestamp": "2024-01-01T00:00:00+00:00"
            }
            """));

        var response = await _client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = JsonSerializer.Deserialize<JsonElement[]>(await response.Content.ReadAsStringAsync())!;
        Assert.Single(data);
        Assert.Equal("CB-07", data[0].GetProperty("circuit_id").GetString());
    }

    [Fact]
    public async Task ApiResponsesAreNotCached()
    {
        var response = await _client.GetAsync("/api/events");

        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task AppliancesEndpointEmpty()
    {
        var response = await _client.GetAsync("/api/appliances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PriceEndpointNoSidecar()
    {
        var response = await _client.GetAsync("/api/price");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
