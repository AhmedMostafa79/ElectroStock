using ElectroStock.Domain.Models;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class InvoiceItemTests
    {
        [Theory]
        [InlineData(5,5,25)]
        [InlineData(5,0.005,0.025)]
        [InlineData(999,100000,99900000)]
        public void Constructor_WithValidData_CreatesInvoiceItem(int quantity,decimal unitPrice,decimal totalPrice)
        {

            // Arrange
            var productId=Guid.NewGuid();
           
            // Act
            var invoiceItem = new InvoiceItem(
                productId:productId,
                quantity:quantity,
                unitPrice: unitPrice
                );

            // Assert
            invoiceItem.ProductId.Should().Be(productId);
            invoiceItem.Quantity.Should().Be(quantity);
            invoiceItem.UnitPrice.Should().Be(unitPrice);
            invoiceItem.TotalPrice().Should().Be(totalPrice);

        }
        [Theory]
        [InlineData(-5, 10)]
        [InlineData(0, 4)]
        public void Constructor_WithInvalidQuantity_ThrowsException(int quantity, decimal unitPrice)
        {

            // Arrange
            var productId = Guid.NewGuid();


            // Act
            var act = () => new InvoiceItem(productId, quantity, unitPrice);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"Quantity must be positive. Received: {quantity}");

        }
        [Theory]
        [InlineData(5, -10)]
        [InlineData(4, 0)]
        public void Constructor_WithInvalidUnitPrice_ThrowsException(int quantity, decimal unitPrice)
        {

            // Arrange
            var productId = Guid.NewGuid();


            // Act
            var act = () => new InvoiceItem(productId, quantity, unitPrice);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"Unit price must be positive. Received: {unitPrice}");

        }
        
    }
}
