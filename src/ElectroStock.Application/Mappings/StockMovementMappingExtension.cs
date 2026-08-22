using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Models;

namespace ElectroStock.Application.Mappings
{
    public static class StockMovementMappingExtension
    {
        public static StockMovementDto ToDto(this StockMovement stockMovement)
        {
            if (stockMovement == null)
                return null;

            return new StockMovementDto
            {
                Id = stockMovement.Id,
                ProductId = stockMovement.ProductId,
                Quantity = stockMovement.Quantity,
                QuantityBefore= stockMovement.QuantityBefore,
                QuantityAfter= stockMovement.QuantityAfter,
                PerformedBy= stockMovement.PerformedBy,
                Details=stockMovement.Details,
                ReferenceType=stockMovement.ReferenceType,
                CreatedAt=stockMovement.CreatedAt
            };
        }
    }
}
