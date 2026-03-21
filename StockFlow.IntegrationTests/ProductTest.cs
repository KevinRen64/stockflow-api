using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.IntegrationTests;

public class ProductTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();

            db.Products.RemoveRange(db.Products);
            await db.SaveChangesAsync();

            db.Products.Add(new Product
            {
                Id = productId,
                Name = "Test Product",
                Sku = "SKU-001"
            });

            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Product");
        content.Should().Contain("SKU-001");
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenProductDoesNotExist()
    {
      var id = Guid.NewGuid();

      var response = await _client.GetAsync($"/api/products/{id}");

      response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}