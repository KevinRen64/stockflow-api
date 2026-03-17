using StockFlow.Application.Common;

namespace StockFlow.Application.Products;

public interface IProductService
{
  Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct);
  Task<ProductDto> GetByIdAsync(Guid id , CancellationToken ct);
}