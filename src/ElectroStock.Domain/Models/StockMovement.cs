using ElectroStock.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
    public class StockMovement
    {

        [Key]
        public Guid Id { get; private set; }

        [Required]
        public Guid ProductId { get; private set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; private set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int QuantityBefore { get; private set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int QuantityAfter { get; private set; }

        public Guid? PerformedBy { get; private set; }

        [MaxLength(500)]
        public string Details { get; private set; }

        [Required]
        public AuditLogType ReferenceType { get; private set; }

        [Required]
        public DateTime CreatedAt { get; private set; }

        public StockMovement(
            Guid productId,
            int quantity,
            int quantityBefore,
            int quantityAfter,
            AuditLogType referenceType,
            Guid? performedBy = null,
            string details = null)
        {
            Id = Guid.NewGuid();
            ProductId = productId;
            Quantity = quantity;
            QuantityBefore = quantityBefore;
            QuantityAfter = quantityAfter;
            ReferenceType = referenceType;
            PerformedBy = performedBy;
            Details = details ?? GetDefaultDetails(referenceType, quantity, quantityBefore, quantityAfter);
            CreatedAt = DateTime.UtcNow;
        }

        private string GetDefaultDetails(AuditLogType type, int quantity, int before, int after)
        {
            return type switch
            {
                AuditLogType.StockReceipt => $"Received {quantity} units. Stock: {before} → {after}",
                AuditLogType.StockSale => $"Sold {quantity} units. Stock: {before} → {after}",
                AuditLogType.StockRefund => $"Refunded {quantity} units. Stock: {before} → {after}",
                _ => $"Stock movement: {quantity} units. Stock: {before} → {after}"
            };
        }
    }
}
