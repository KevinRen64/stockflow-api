using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace StockFlow.IntegrationTests;

public class ApiSmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnknownRoute_ShouldReturn404()
    {
        var response = await _client.GetAsync("/api/not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}