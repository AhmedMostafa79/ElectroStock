using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class StockMovementTests
    {
        [Fact]
        public void Constructor_WithValidData_CreatesStockMovement()
        {
            // Arrange
            var quantity = 10;
            var quantityBefore = 100;
            var quantityAfter =110;
            var movementType = AuditLogType.StockReceipt ;
            var performedBy = Guid.NewGuid();
            var details = $"Receiver quantity {quantity}, " +
                "quantity before: {quantityBefore}, quantity after: {quantityAfter}," +
                "performed by: {performedBy}";

            // Act
            var stockMovement = new StockMovement(
                productId: Guid.NewGuid(),
                quantity: quantity,
                quantityBefore: quantityBefore,
                quantityAfter: quantityAfter,
                referenceType: movementType,
                performedBy: performedBy,
                details: details
                );

            // Assert
            stockMovement.Quantity.Should().Be(quantity );
            stockMovement.QuantityBefore.Should().Be(quantityBefore);
            stockMovement.QuantityAfter.Should().Be(quantityAfter);
            stockMovement.ReferenceType.Should().Be( movementType );
            stockMovement.PerformedBy.Should().Be(performedBy);
            stockMovement.Details.Should().Be(details);
        }
        [Fact]
        public void Constructor_WithNoGivenDetails_CreatesStockMovement()
        {

            // Arrange
            var quantity = 10;
            var quantityBefore = 100;
            var quantityAfter =110;
            var movementType = AuditLogType.StockReceipt ;
            var performedBy = Guid.NewGuid();

            // Act
            var stockMovement = new StockMovement(
                productId: Guid.NewGuid(),
                quantity: quantity,
                quantityBefore: quantityBefore,
                quantityAfter: quantityAfter,
                referenceType: movementType,
                performedBy: performedBy
                );

            // Assert
            stockMovement.Quantity.Should().Be(quantity );
            stockMovement.QuantityBefore.Should().Be(quantityBefore);
            stockMovement.QuantityAfter.Should().Be(quantityAfter);
            stockMovement.ReferenceType.Should().Be( movementType );
            stockMovement.PerformedBy.Should().Be(performedBy);
            // when no details given, details should be set by GetDefaultDetails()
            stockMovement.Details.Should().NotBeNull();
        }
    }
}
