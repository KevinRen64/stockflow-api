using System.Data.Common;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Application.Products;

public class ProductService : IProductService
{
  private readonly StockFlowDbContext _db;

  public ProductService(StockFlowDbContext db)
  {
    _db = db;
  }

  public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct)
  {
    var normalizedSku = req.Sku.Trim();
    var normalizedName = req.Name.Trim();
    
    var exists = await _db.Products.AnyAsync( p => p.Sku == req.Sku, ct);

    if(exists)
    {
      return Result<ProductDto>.Failure($"SKU '{normalizedSku}' already exists.", "duplicate_sku");
    }

    var product = new Product
    {
      Sku = normalizedSku,
      Name = normalizedName,
      IsActive = true,
      CreatedAt = DateTimeOffset.UtcNow
    };

    _db.Products.Add(product);
    await _db.SaveChangesAsync(ct);

    return Result<ProductDto>.Success(MapToDto(product));
  }

  public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct)
  {
    var product = await _db.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.Id == id, ct);

    if(product is null)
    {
      return Result<ProductDto>.Failure("Product not found. ", "product_not_found");
    }
    return Result<ProductDto>.Success(MapToDto(product));
  }

  private static ProductDto MapToDto(Product product)
  {
    return new ProductDto(
      product.Id,
      product.Sku,
      product.Name,
      product.IsActive,
      product.CreatedAt
    );
  }
}