using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Exceptions;
using StockFlow.Application.Products;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;
using Xunit;

namespace StockFlow.UnitTests.Products;

public class ProductServiceTests
{
  private static StockFlowDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    
    return new StockFlowDbContext(options);
  }
  
  [Fact]
  public async Task CreateAsync_Should_Create_Product_When_Sku_Does_Not_Exist()
  {
    await using var db = CreateDbContext();
    var service = new ProductService(db);

    var request = new CreateProductRequest(" IP-001 ", " iPhone 13 ");

    // Act
    var result = await service.CreateAsync(request, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();

    result.Value!.Sku.Should().Be("IP-001");
    result.Value!.Name.Should().Be("iPhone 13");
    result.Value.IsActive.Should().BeTrue();

    var productInDb = await db.Products.FirstOrDefaultAsync(p => p.Sku == "IP-001");
    productInDb.Should().NotBeNull();
    productInDb!.Name.Should().Be("iPhone 13");
    productInDb.IsActive.Should().BeTrue();
  }

  [Fact]
  public async Task CreateAsync_Should_Return_Failure_When_Sku_Already_Exists()
  {
    // Arrange
    await using var db = CreateDbContext();

    db.Products.Add(new Product
    {
      Sku = "IP-001",
      Name = "Existing Product",
      IsActive = true,
      CreatedAt = DateTimeOffset.UtcNow
    });

    await db.SaveChangesAsync();

    var service = new ProductService(db);

    var request = new CreateProductRequest("IP-001", " New Product ");

    // Act
    var result = await service.CreateAsync(request, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ErrorCode.Should().Be("duplicate_sku");
    result.Error.Should().Contain("IP-001");

    var count = await db.Products.CountAsync();
    count.Should().Be(1);
  }

  [Fact]
  public async Task GetByIdAsync_Should_Return_Product_When_Product_Exists()
  {
    // Arrange
    await using var db = CreateDbContext();

    var product = new Product
    {
      Sku = "IP-001",
      Name = "iPhone 13",
      IsActive = true,
      CreatedAt = DateTimeOffset.UtcNow
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    var service = new ProductService(db);

    // Act
    var result = await service.GetByIdAsync(product.Id, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(product.Id);
    result.Sku.Should().Be("IP-001");
    result.Name.Should().Be("iPhone 13");
    result.IsActive.Should().BeTrue();
  }

  [Fact]
  public async Task GetByIdAsync_Should_Throw_NotFoundException_When_Product_Does_Not_Exist()
  {
    // Arrange
    await using var db = CreateDbContext();
    var service = new ProductService(db);

    var id = Guid.NewGuid();

    // Act
    Func<Task> act = async() => await service.GetByIdAsync(id, CancellationToken.None);

    // Assert
    var exception = await act.Should().ThrowAsync<NotFoundException>();
    exception.Which.Code.Should().Be("product_not_found");
    exception.Which.Message.Should().Be("Product not found.");
  }
}