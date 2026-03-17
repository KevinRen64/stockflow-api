using StockFlow.Application.Common;

namespace StockFlow.Application.Products;

public interface IProductService
{
  Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct);
  Task<Result<ProductDto>> GetByIdAsync(Guid id , CancellationToken ct);
}