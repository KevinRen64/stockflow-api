using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Application.Common;
using StockFlow.Application.Common.Exceptions;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Application.Inventory;

public class InventoryService : IInventoryService
{
    private readonly StockFlowDbContext _db;

    public InventoryService(StockFlowDbContext db)
    {
        _db = db;
    }

    public async Task<Result<InventoryDto>> AdjustAsync(AdjustInventoryRequest request, CancellationToken ct)
    {
        if (request.QuantityDelta == 0)
        {
            return Result<InventoryDto>.Failure(
                "QuantityDelta cannot be zero.",
                "invalid_quantity_delta");
        }

        var productExists = await _db.Products
            .AnyAsync(p => p.Id == request.ProductId, ct);

        if (!productExists)
        {
            throw new NotFoundException(
                "Product not found.",
                "product_not_found");
        }

        var inventory = await _db.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, ct);

        var now = DateTimeOffset.UtcNow;

        try
        {
            if (inventory is null)
            {
                if (request.QuantityDelta < 0)
                {
                    return Result<InventoryDto>.Failure(
                        "Cannot reduce stock before inventory exists.",
                        "inventory_not_initialized");
                }

                inventory = new InventoryItem
                {
                    ProductId = request.ProductId,
                    OnHand = request.QuantityDelta,
                    Reserved = 0,
                    UpdatedAt = now
                };

                _db.InventoryItems.Add(inventory);

                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = request.ProductId,
                    QtyDelta = request.QuantityDelta,
                    Type = "Adjust",
                    RefId = null,
                    CreatedAt = now
                });

                Log.Information(
                    "Inventory initialized for ProductId {ProductId}, OnHand {OnHand}, Reserved {Reserved}",
                    inventory.ProductId,
                    inventory.OnHand,
                    inventory.Reserved);
            }
            else
            {
                var oldOnHand = inventory.OnHand;
                var oldReserved = inventory.Reserved;
                var newOnHand = inventory.OnHand + request.QuantityDelta;

                if (newOnHand < 0)
                {
                    return Result<InventoryDto>.Failure(
                        "OnHand cannot be negative.",
                        "negative_inventory");
                }

                if (newOnHand < inventory.Reserved)
                {
                    return Result<InventoryDto>.Failure(
                        "OnHand cannot be less than Reserved.",
                        "inventory_below_reserved");
                }

                inventory.OnHand = newOnHand;
                inventory.UpdatedAt = now;

                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = request.ProductId,
                    QtyDelta = request.QuantityDelta,
                    Type = "Adjust",
                    RefId = null,
                    CreatedAt = now
                });

                Log.Information(
                    "Inventory adjusted for ProductId {ProductId}, Delta {Delta}, OldOnHand {OldOnHand}, NewOnHand {NewOnHand}, Reserved {Reserved}",
                    request.ProductId,
                    request.QuantityDelta,
                    oldOnHand,
                    inventory.OnHand,
                    oldReserved);
            }

            await _db.SaveChangesAsync(ct);

            return Result<InventoryDto>.Success(MapToDto(inventory));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Log.Warning(
                ex,
                "Inventory concurrency conflict for ProductId {ProductId}",
                request.ProductId);

            throw new ConflictException(
                "Inventory was updated by another request. Please retry.",
                "inventory_conflict");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            Log.Warning(
                ex,
                "Inventory initialization conflict for ProductId {ProductId}",
                request.ProductId);

            throw new ConflictException(
                "Inventory already exists for this product.",
                "inventory_already_initialized");
        }
    }

    public async Task<InventoryDto> GetByProductIdAsync(Guid productId, CancellationToken ct)
    {
        var inventory = await _db.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId, ct);

        if (inventory is null)
        {
            throw new NotFoundException(
                "Inventory not found.",
                "inventory_not_found");
        }

        return MapToDto(inventory);
    }

    private static InventoryDto MapToDto(InventoryItem inventory)
    {
        return new InventoryDto(
            inventory.Id,
            inventory.ProductId,
            inventory.OnHand,
            inventory.Reserved,
            inventory.OnHand - inventory.Reserved,
            inventory.UpdatedAt
        );
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("IX_InventoryItems_ProductId", StringComparison.OrdinalIgnoreCase) == true;
    }
}