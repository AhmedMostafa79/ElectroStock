using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Dtos
{
    public class StockMovementDto
    {
        public Guid Id { get; init; }

        public Guid ProductId { get; init; }

        public int Quantity { get; init; }

        public int QuantityBefore { get; init; }

        public int QuantityAfter { get; init; }

        public Guid? PerformedBy { get; init; }

        public string? Details { get; init; }

        public AuditLogType ReferenceType { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
