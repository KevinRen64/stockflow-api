using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Application.Common;
using StockFlow.Application.Common.Exceptions;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Application.Orders;

public class OrderService : IOrderService
{
  private readonly StockFlowDbContext _db;

  public OrderService(StockFlowDbContext db)
  {
    _db = db;
  }

public async Task<Result<OrderDto>> CreateAsync(CreateOrderRequest req, string idempotencyKey, CancellationToken ct)
{
    idempotencyKey = idempotencyKey.Trim();

    if (string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Result<OrderDto>.Failure(
            "Idempotency-Key is required.",
            "missing_idempotency_key");
    }

    var requestHash = ComputeRequestHash(req);

    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    try
    {
        try
        {
            _db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Key = idempotencyKey,
                RequestType = "CreateOrder",
                RequestHash = requestHash,
                Status = "InProgress",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await tx.RollbackAsync(ct);

            var existing = await _db.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Key == idempotencyKey && x.RequestType == "CreateOrder",
                    ct);

            if (existing == null)
            {
                throw;
            }

            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                Log.Warning(
                    "Idempotency key payload mismatch for Key {IdempotencyKey}",
                    idempotencyKey);

                return Result<OrderDto>.Failure(
                    "This Idempotency-Key was already used with a different request.",
                    "idempotency_key_payload_mismatch");
            }

            if (existing.Status == "Completed" && existing.OrderId.HasValue)
            {
                var existingOrder = await _db.Orders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == existing.OrderId.Value, ct);

                if (existingOrder != null)
                {
                    Log.Information(
                        "Idempotent replay detected for Key {IdempotencyKey}, returning existing OrderId {OrderId}",
                        idempotencyKey,
                        existingOrder.Id);

                    return Result<OrderDto>.Success(MapToDto(existingOrder));
                }
            }

            if (existing.Status == "InProgress")
            {
                Log.Information(
                    "Idempotent request already in progress for Key {IdempotencyKey}",
                    idempotencyKey);

                return Result<OrderDto>.Failure(
                    "This request is already being processed.",
                    "request_in_progress");
            }

            return Result<OrderDto>.Failure(
                "A previous request with this Idempotency-Key failed. Please use a new key.",
                "idempotency_request_failed");
        }

        var createResult = await CreateOrderInternalAsync(req, ct);

        var record = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(
                x => x.Key == idempotencyKey && x.RequestType == "CreateOrder",
                ct);

        if (record == null)
        {
            throw new InvalidOperationException("Idempotency record not found after order creation.");
        }

        if (!createResult.IsSuccess)
        {
            record.Status = "Failed";
            record.CompletedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return createResult;
        }

        record.Status = "Completed";
        record.OrderId = createResult.Value!.Id;
        record.CompletedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return createResult;
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
}

  public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken ct)
  {
    var order = await _db.Orders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);

    if(order == null)
    {
      throw new NotFoundException("Order not found.", "order_not_found");
    }

    return MapToDto(order);
  }

  public async Task<PagedResult<OrderDto>> GetAllAsync(string? status, string? keyword, int page, int pageSize, string? sortBy, bool desc, CancellationToken ct)
  {
    page = page < 1 ? 1 : page;
    pageSize = pageSize < 1 ? 10 : pageSize;
    pageSize = pageSize > 100 ? 100 : pageSize;

    var query = _db.Orders
        .Include(x => x.Lines)
        .AsNoTracking()
        .AsQueryable();

    if(!string.IsNullOrWhiteSpace(status))
    {
      query = query.Where(x => x.Status == status);
    }

    if(!string.IsNullOrWhiteSpace(keyword))
    {
      query = query.Where(x =>
        x.OrderNumber.Contains(keyword) ||
        x.CustomerName.Contains(keyword)
      );
    }

    query = (sortBy?.ToLower()) switch
    {
      "createdat" => desc
        ? query.OrderByDescending(x => x.CreatedAt)
        : query.OrderBy(x => x.CreatedAt),

      "status" => desc
        ? query.OrderByDescending(x => x.Status)
        : query.OrderBy(x => x.Status),

      "id" => desc
        ? query.OrderByDescending(x => x.Id)
        : query.OrderBy(x => x.Id),

      _ => desc
        ? query.OrderByDescending(x => x.CreatedAt)
        : query.OrderBy(x => x.CreatedAt)
    };

    var totalCount = await query.CountAsync();

    var orders = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);
    
    var items = orders.Select(MapToDto).ToList();

    return PagedResult<OrderDto>.Create(items, page, pageSize, totalCount);
  }
  
  public async Task<OrderDto> CancelAsync(Guid id, CancellationToken ct)
  {
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    try
    {
      
      var order = await _db.Orders
          .Include(x => x.Lines)
          .FirstOrDefaultAsync(x => x.Id == id, ct);
      
      if(order == null)
      {
        throw new NotFoundException("Order not found.", "order_not_found");
      }

      if(order.Status != "Pending")
      {
        throw new ConflictException("Only pending orders can be cancelled.", "invalid_order_status");
      }

      // Cancel order logic
      foreach(var line in order.Lines)
      {
        var inventory = await _db.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductId == line.ProductId, ct);
        
        if(inventory == null)
        {
          throw new NotFoundException(
            $"Inventory not found for product {line.ProductId}.",
            "inventory_not_found");
        }

        if (inventory.Reserved < line.Quantity)
        {
            throw new ConflictException(
                "Reserved stock is less than the order quantity.",
                "invalid_reserved_stock");
        }

        inventory.Reserved -= line.Quantity;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;

        _db.StockMovements.Add(new StockMovement
        {
          ProductId = line.ProductId,
          QtyDelta = line.Quantity,
          Type = "Release",
          RefId = order.Id.ToString(),
          CreatedAt = DateTimeOffset.UtcNow
        });
      }

      order.Status = "Cancelled";
      await _db.SaveChangesAsync(ct);
      await tx.CommitAsync(ct);

      Log.Information(
        "Order cancelled {OrderId} {OrderNumber}",
        order.Id,
        order.OrderNumber);

      return MapToDto(order);
    }

    catch(DbUpdateConcurrencyException ex)
    {
      await tx.RollbackAsync(ct);

      Log.Warning(
        ex,
        "Order cancellation concurrency conflict for OrderId {OrderId}",
        id);

      throw new ConflictException(
        "The inventory was updated by another request. Please retry.",
        "inventory_concurrency_conflict");
    }

    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
  }
  
  public async Task<OrderDto> ConfirmAsync(Guid id, CancellationToken ct)
  {
    await using var tx = await _db.Database.BeginTransactionAsync(ct);
    try
    {
      
      var order = await _db.Orders
          .Include(x => x.Lines)
          .FirstOrDefaultAsync(x => x.Id == id, ct); 

      if(order == null)
      {
        throw new NotFoundException("Order not found.", "order_not_found");
      }

      if(order.Status != "Pending")
      {
        throw new ConflictException("Only pending orders can be confirmed.", "invalid_order_status");
      }

      // Confirm order logic
      foreach(var line in order.Lines)
      {
        var inventory = await _db.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductId == line.ProductId, ct);
        
        if(inventory == null)
        {
          throw new NotFoundException(
            $"Inventory not found for product {line.ProductId}.",
            "inventory_not_found");        
        }

        if(inventory.Reserved < line.Quantity)
        {
          throw new ConflictException(
            "Reserved stock is less than the order quantity.",
            "invalid_reserved_stock");
        }

        if(inventory.OnHand < line.Quantity)
        {
          throw new ConflictException(
            "On-hand stock is less than the order quantity.",
            "insufficient_onhand_stock");
        }

        inventory.Reserved -= line.Quantity;
        inventory.OnHand -= line.Quantity;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;

        _db.StockMovements.Add(new StockMovement{
          ProductId = line.ProductId,
          QtyDelta = -line.Quantity,
          Type = "Deduct",
          RefId = order.Id.ToString(),
          CreatedAt = DateTimeOffset.UtcNow
        });
      }

      order.Status = "Confirmed";

      await _db.SaveChangesAsync(ct);
      await tx.CommitAsync(ct);

      Log.Information(
        "Order confirmed {OrderId} {OrderNumber}",
        order.Id,
        order.OrderNumber);

      return MapToDto(order);
    }
    catch(DbUpdateConcurrencyException ex)
    {
      await tx.RollbackAsync(ct);

      Log.Warning(
        ex,
        "Order confirmation concurrency conflict for OrderId {OrderId}",
        id);

      throw new ConflictException(
        "The inventory was updated by another request. Please retry.",
        "inventory_concurrency_conflict");
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
  }

  public async Task<OrderDto> ShipAsync(Guid id, CancellationToken ct)
  {
    var order = await _db.Orders
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, ct);
    
    if(order == null)
    {
      throw new NotFoundException("Order not found.", "order_not_found");
    }

    if(order.Status != "Confirmed")
    {
      throw new ConflictException("Only confirmed orders can be shipped.", "invalid_order_status");
    }

    order.Status = "Shipped";


    try
    {
      await _db.SaveChangesAsync(ct);
      Log.Information(
      "Order shipped {OrderId} {OrderNumber}",
      order.Id,
      order.OrderNumber);
    }
    catch (DbUpdateConcurrencyException ex)
    {
      Log.Warning(
        ex,
        "Order shipping conflict for OrderId {OrderId}",
        order.Id);

      throw new ConflictException(
        "Order was updated by another request. Please retry.",
        "order_conflict");
    }

    return MapToDto(order);
  }

  public async Task<OrderDto> CompleteAsync(Guid id, CancellationToken ct)
  {
    var order = await _db.Orders
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, ct);
    
    if(order == null)
    {
      throw new NotFoundException("Order not found.", "order_not_found");
    }

    if(order.Status != "Shipped")
    {
      throw new ConflictException("Only shipped orders can be completed.", "invalid_order_status");
    }

    order.Status = "Completed";

    try
    {  
      await _db.SaveChangesAsync(ct);

      Log.Information(
        "Order completed {OrderId} {OrderNumber}",
        order.Id,
        order.OrderNumber);
    }
    catch (DbUpdateConcurrencyException ex)
    {
      Log.Warning(
        ex,
        "Order completion conflict for OrderId {OrderId}",
        order.Id);

      throw new ConflictException("Order was updated by another request. Please retry. ", "order_conflict");
    }

    return MapToDto(order);
  }

  private static OrderDto MapToDto(Order order)
  {
    return new OrderDto(
      order.Id,
      order.OrderNumber,
      order.Status,
      order.CustomerName,
      order.TotalAmount,
      order.CreatedAt,
      order.Lines.Select(l => new OrderLineDto(
        l.ProductId,
        l.Quantity,
        l.UnitPrice
      )).ToList()
    );
  }

  private static string GenerateOrderNumber()
  {
    return $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
  }

  private static string ComputeRequestHash(CreateOrderRequest req)
  {
    var raw = $"{req.ProductId}|{req.Quantity}|{req.CustomerName.Trim()}";
    var bytes = Encoding.UTF8.GetBytes(raw);
    var hashBytes = SHA256.HashData(bytes);
    return Convert.ToHexString(hashBytes);
  }

  private static bool IsUniqueConstraintViolation(DbUpdateException ex)
  {
    return ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("IX_IdempotencyRecords_Key_RequestType", StringComparison.OrdinalIgnoreCase) == true;  
  }

  private async Task<Result<OrderDto>> CreateOrderInternalAsync(CreateOrderRequest req, CancellationToken ct)
{
    var product = await _db.Products
        .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

    if (product == null)
    {
        return Result<OrderDto>.Failure(
            "Product not found.",
            "product_not_found");
    }

    var inventory = await _db.InventoryItems
        .FirstOrDefaultAsync(x => x.ProductId == req.ProductId, ct);

    if (inventory == null)
    {
        return Result<OrderDto>.Failure(
            "Inventory not found for product.",
            "inventory_not_found");
    }

    var available = inventory.OnHand - inventory.Reserved;

    if (available < req.Quantity)
    {
        return Result<OrderDto>.Failure(
            "Insufficient stock.",
            "insufficient_stock");
    }

    var now = DateTimeOffset.UtcNow;
    var unitPrice = 10.00m; // fixed for now
    var totalAmount = unitPrice * req.Quantity;

    var order = new Order
    {
        OrderNumber = GenerateOrderNumber(),
        Status = "Pending",
        CustomerName = req.CustomerName.Trim(),
        TotalAmount = totalAmount,
        CreatedAt = now,
        Lines = new List<OrderLine>
        {
            new OrderLine
            {
                ProductId = req.ProductId,
                Quantity = req.Quantity,
                UnitPrice = unitPrice
            }
        }
    };

    inventory.Reserved += req.Quantity;
    inventory.UpdatedAt = now;

    _db.Orders.Add(order);

    _db.StockMovements.Add(new StockMovement
    {
        ProductId = req.ProductId,
        QtyDelta = -req.Quantity,
        Type = "Reserve",
        RefId = order.Id.ToString(),
        CreatedAt = now
    });

    try
    {
        await _db.SaveChangesAsync(ct);

        Log.Information(
            "Order created {OrderId} {OrderNumber} for ProductId {ProductId}, Quantity {Quantity}, TotalAmount {TotalAmount}",
            order.Id,
            order.OrderNumber,
            req.ProductId,
            req.Quantity,
            totalAmount);

        return Result<OrderDto>.Success(MapToDto(order));
    }
    catch (DbUpdateConcurrencyException ex)
    {
        Log.Warning(
            ex,
            "Order creation concurrency conflict for ProductId {ProductId}",
            req.ProductId);

        return Result<OrderDto>.Failure(
            "The inventory was updated by another request. Please retry.",
            "inventory_concurrency_conflict");
    }
}
}