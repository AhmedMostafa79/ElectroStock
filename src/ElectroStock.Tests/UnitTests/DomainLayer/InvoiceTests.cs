using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class InvoiceTests
    {
        
        private Invoice CreateTestInvoice()
        {
            var invoice = new Invoice(
                   invoiceNumber: $"INV-20260523-0000",
            customerId: Guid.NewGuid(),
                   orderSource: RequestSource.InStore,
            createdBy: Guid.NewGuid(),
                   paymentMethod: PaymentMethod.Cash
                   );
            return invoice;
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInvoice()
        {
            // Arrange
            var customerId= Guid.NewGuid();
            var orderSource = RequestSource.InStore;
            var performedBy = Guid.NewGuid();
            var paymentMethod = PaymentMethod.Cash;

            // Act
            var invoice = new Invoice(
                   invoiceNumber: $"INV-{Guid.NewGuid():N}",
            customerId: customerId,
                   orderSource: orderSource,
            createdBy: performedBy,
                   paymentMethod: paymentMethod
                   );

            //Assert
            invoice.Should().NotBeNull();
            invoice.CustomerId.Should().Be(customerId);
            invoice.OrderSource.Should().Be(orderSource);
            invoice.CreatedBy.Should().Be(performedBy);
            invoice.PaymentMethod.Should().Be(paymentMethod);
        }

        [Theory]
        [InlineData(5,10,50)]
        [InlineData(4,3,12)]
        [InlineData(5,4.99,24.95)]
        public void AddItem_WithValidData_AddsItemToInvoice( int quantity, decimal unitPrice,decimal expectedTotal)
        {

            // Arrange
            var invoice = CreateTestInvoice();
            var productId= Guid.NewGuid();


            // Act
            invoice.AddItem(productId, quantity, unitPrice);

            // Assert
            invoice.Items.Should().HaveCount(1);
            invoice.TotalPrice.Should().Be(expectedTotal);

            var addedItem = invoice.Items.First();

            addedItem.ProductId.Should().Be(productId);
            addedItem.Quantity.Should().Be(quantity);
            addedItem.UnitPrice.Should().Be(unitPrice);
        }
        [Fact]
        public void AddItem_WhenStatusNotPending_ThrowsException()
        {

            // Arrange
            var invoice =  CreateTestInvoice();
            var productId = Guid.NewGuid();

            invoice.Cancel();

            // Act
            var act = () => invoice.AddItem(productId, quantity:5,unitPrice: 5);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Invoice is {invoice.Status}, can't add items to invoice!");
            invoice.Items.Should().BeEmpty();
        
        }

        [Theory]
        [InlineData(-5,10)]
        [InlineData(0,3)]
        public void AddItem_WithInvalidQuantity_ThrowsException( int quantity, decimal unitPrice)
        {

            // Arrange
            var invoice = CreateTestInvoice();
            var productId= Guid.NewGuid();


            // Act
            var act=()=>invoice.AddItem(productId, quantity, unitPrice);

            // Assert

            invoice.Items.Should().BeEmpty();

        }

        [Theory]
        [InlineData(5,-10)]
        [InlineData(4,0)]
        public void AddItem_WithInvalidUnitPrice_ThrowsException( int quantity, decimal unitPrice)
        {

            // Arrange
            var invoice = CreateTestInvoice();
            var productId= Guid.NewGuid();


            // Act
            var act=()=>invoice.AddItem(productId, quantity, unitPrice);

            // Assert

            invoice.Items.Should().BeEmpty();
        }
        [Fact]
        public void RemoveItem_WithExistingItem_RemovesItem()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var quantity = 5;
            var unitPrice = 5;

            var invoice=  CreateTestInvoice();

            invoice.AddItem(productId, quantity, unitPrice);

            // Act
            invoice.RemoveItem(productId);

            // Assert
            invoice.Items.Should().BeEmpty();
            invoice.TotalPrice.Should().Be(0);
        }
        [Fact]
        public void RemoveItem_WithNonExistingItem_ThrowsException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var quantity = 5;
            var unitPrice = 5;

            var invoice=  CreateTestInvoice();

            invoice.AddItem(productId, quantity, unitPrice);

            var anotherId = Guid.NewGuid();
            // Act
           var act=()=> invoice.RemoveItem(anotherId);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Product {anotherId} not found in invoice");

            invoice.Items.Should().HaveCount(1);

        }

    }
}
