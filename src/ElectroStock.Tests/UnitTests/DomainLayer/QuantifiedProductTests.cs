using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class QuantifiedProductTests
    {
        private ProductCreationData CreateTestData()
        {
            return new ProductCreationData
            {
                Sku = "OR-OFP-500mahWhite",
                Brand = "Oraimo",
                Name = "OraimoFreepods",
                Category = ProductCategory.Audio,
                Model = "OEB-E104DC",
                UnitCost = 1000,
                UnitPrice = 1200,
            };
        }
        [Fact]
        public void Constructor_WithValidData_CreatesProduct()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 5;
            var minimumStockLevel = 10;
            // Act
            var product = new QuantifiedProduct(
                productData: data,
                initialQuantity: initialQuantity,
                minimumStockLevel: minimumStockLevel
                );

            // Assert // validating only QuantifiedProduct related properties
            product.ProductType.Should().Be(ProductType.Quantified);
            product.QuantityOnHand.Should().Be(initialQuantity);
            product.MinimumStockLevel.Should().Be(minimumStockLevel);
            product.QuantityReserved.Should().Be(0);
            product.AvailableQuantity.Should().Be(product.QuantityOnHand);
        }

        [Theory]
        [InlineData(-1)]
        public void Constructor_WithInvalidQuantity_ThrowsException(int initialQuantity)
        {
            // Arrange
            var data = CreateTestData();
            // Act
            var act = () => new QuantifiedProduct(
                productData: data,
                initialQuantity: initialQuantity,
                minimumStockLevel: 10
                );

            // Assert
            act.Should().Throw<ArgumentException>()
                 .WithMessage($"Quantity can't be negative. Received: {initialQuantity}");
        }

        [Fact]
        public void CanReserveOrSell_ReturnsTrueIfCanReserveOrSell()
        {
            // Arrange
            var data = CreateTestData();
            var product = new QuantifiedProduct(
                productData: data,
                initialQuantity: 10,
                minimumStockLevel: 5
                );

            // Act & Assert
            product.CanReserveOrSell(1).Should().Be(false); //ProductStatus=Draft "initially"->returns false

            // Act & Assert
            product.Publish();//ProductStatus=Active
            product.CanReserveOrSell(1).Should().Be(true);

        }

        [Fact]
        public void Reserve_WithValidQuantity_ReservesQuantity()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );
            var quantityToReserve = 10;
            product.Publish();

            // Act
            product.Reserve(quantityToReserve);

            // Reserve
            product.QuantityReserved.Should().Be(quantityToReserve);
            product.AvailableQuantity.Should().Be(product.QuantityOnHand - product.QuantityReserved);
        }

        [Fact]
        public void Reserve_WithUnavailableQuantity_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();
            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );
            var quantityToReserve = 11;
            product.Publish();

            // Act
            var act = () => product.Reserve(quantityToReserve);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Product is not available or quantity is not sufficient");

            product.QuantityReserved.Should().Be(0);
        }

        [Fact]
        public void ReleaseReservation_WithValidQuantity_ReleasesQuantity()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );

            var quantityToReserv1 = 5;
            var quantityToKeepReserved = 1;
            product.Publish();

            // Act & Assert 1
            product.Reserve(quantityToReserv1); //reserve 5, quantityToReserv1= 5
            product.AvailableQuantity.Should().Be(initialQuantity - quantityToReserv1);// available = 10-5=5;
            product.ReleaseReservation(quantityToReserv1 - quantityToKeepReserved); //released 5-1=4,
            product.QuantityReserved.Should().Be(quantityToKeepReserved);// kept reserved=1


            // Act & Assert 2
            var quantityToReserve2 = 6;
            product.Reserve(quantityToReserve2);// reserve extra 6= ReservedQuantity+quantityToReserve= 1+6=7
            product.QuantityReserved.Should().Be(quantityToReserve2 + quantityToKeepReserved);
        }

        [Fact]
        public void ReleaseReservation_WithInvalidQuantity_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );

            var quantityToReserve = 10;
            product.Publish();
            product.Reserve(quantityToReserve);
            var quantityToRelease = quantityToReserve + 1;

            // Act
            var act = () => product.ReleaseReservation(quantityToRelease); // release 11 while reserved 10

            // Reserve
            act.Should().Throw<InvalidOperationException>()
                 .WithMessage($"Cannot release {quantityToRelease}. Only {product.QuantityReserved} reserved");

            product.QuantityReserved.Should().Be(quantityToReserve);
        }

        [Fact]
        public void ConfirmSale_WithAvailableQuantity_ConfirmsSale()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );
            product.Publish();

            // Act & Assert 1

            var quantityToReserve = 5;
            var quantityToSell1 = 2;

            product.Reserve(5); //  reserved=5, available=5, onHand=10
            product.QuantityOnHand.Should().Be(initialQuantity);// QuantityAvailable= OnHand-Reserve =10-5=5, 
            product.ConfirmSale(quantityToSell1);// OnHand-ToSell = 10-2=8; 

            product.AvailableQuantity.Should().Be(initialQuantity - quantityToReserve - quantityToSell1); // available=OnHand-Reserved= 8-5= 3
            product.QuantityOnHand.Should().Be(initialQuantity - quantityToSell1);// OnHand=8, reserved=5, available=3

            // Act & Assert 2
            product.ReleaseReservation(quantityToReserve); // release 5 ;
            product.QuantityOnHand.Should().Be(initialQuantity - quantityToSell1); //Onhand + reserved = 3 + 5 = 8

            // as there no reserved, then AvailableQuantity = QuantityOnHand =8
            product.AvailableQuantity.Should().Be(initialQuantity - quantityToSell1);

            var quantityToSell2 = product.AvailableQuantity;// sell all available quantity

            product.ConfirmSale(quantityToSell2);

            product.QuantityOnHand.Should().Be(initialQuantity - (quantityToSell1 + quantityToSell2));
            product.AvailableQuantity.Should().Be(initialQuantity - (quantityToSell1 + quantityToSell2));

            // as there is QuantityOnHand becomes 0, so IsOutOfStock=true
            product.IsOutOfStock.Should().BeTrue();
        }

        [Fact]
        public void ConfirmSale_WithUnavailableQuantity_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );
            product.Publish();

            var quantityToReserve = 9;
            var quantityToSell1 = 5;

            // Act & Assert
            product.Reserve(quantityToReserve);// Available=OnHand-Reserved =10-9=1
            var act = () => product.ConfirmSale(quantityToSell1);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Product is not available or quantity is not sufficient");

            // Act & Assert
            product.ReleaseReservation(quantityToReserve); //Reserved-9= 9-9=0; OnHand=10

            var quantityToSell2 = 10;
            product.ConfirmSale(quantityToSell1);//OnHand-quantityToSell1= 10-5=5

            var act2 = () => product.ConfirmSale(quantityToSell2);//Onhand-quantityToSell2= 5-10. error

            act2.Should().Throw<InvalidOperationException>()
                           .WithMessage("Product is not available or quantity is not sufficient");
        }
        [Fact]
        public void ConfirmReservedSale_WithValidQuantity_ConfirmsSale()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );
            var quantityToReserve = 5;
            var quantityToConfirmSale = 3;
            product.Publish();

            // Act & Assert 1
            product.Reserve(quantityToReserve);// reserved=5

            product.ConfirmReservedSale(quantityToConfirmSale); 
            product.QuantityOnHand.Should().Be(initialQuantity - quantityToConfirmSale);// OnHand = 10 - 3 = 7
            product.QuantityReserved.Should().Be(quantityToReserve- quantityToConfirmSale);// Reserved= 5 - 3 =2
            product.Status.Should().Be(ProductStatus.Active);

            // Act & Assert 2
            product.Reserve(product.AvailableQuantity); //reserve remained available quantity, reserved=OnHand=7

            product.ConfirmReservedSale(product.QuantityReserved);// confirm sale of all reserved, confirmed=7

            // both should be 0
            product.QuantityReserved.Should().Be(0);
            product.QuantityOnHand.Should().Be(0);

        }

        [Fact]
        public void ConfirmReservedSale_WithUnavailableQuantity_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );
            product.Publish();

            var quantityToReserve = 9;
            var quantityToConfirm1 = 10;
            product.Reserve(quantityToReserve);// Available=OnHand-Reserved =10-9=1

            // Act & Assert 1 //insufficient quantity
            var act = () => product.ConfirmReservedSale(quantityToConfirm1);//reserved=9, confirm 10 should throw

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Cannot confirm sale for {quantityToConfirm1}. Only {quantityToReserve} reserved");

            // Act & Assert 2 // product is not Active
            product.Discontinue();

            product.ReleaseReservation(product.QuantityReserved); // quantityOnHand=10

            var act2 = () => product.ConfirmReservedSale(1);// reserve white status=discontinued
            act2.Should().Throw<InvalidOperationException>()
                .WithMessage("Product is not available or quantity is not sufficient");
        }

        [Fact]
        public void ReceiveQuantity_WhenNotDiscontinued_IncreasesQuantityOnHand()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 0;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );

            var quantityToReceive = 5;

            // Act & Assert 1 // Status="Draft" 
            product.QuantityOnHand.Should().Be(initialQuantity);
            product.IsOutOfStock.Should().BeTrue();//initial=0

            product.ReceiveQuantity(quantityToReceive);
            product.QuantityOnHand.Should().Be(initialQuantity + quantityToReceive);
            product.QuantityReserved.Should().Be(0);//no reserved
            product.IsOutOfStock.Should().BeFalse();

            // Act & Assert 2 // Status="Active" //receives
            product.Publish();
            product.ReceiveQuantity(quantityToReceive); //receives
            product.QuantityOnHand.Should().Be(initialQuantity + quantityToReceive * 2);
        }
        [Fact]
        public void ReceiveQuantity_WhenDiscontinued_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();

            var initialQuantity = 10;
            var product = new QuantifiedProduct(
                 productData: data,
                 initialQuantity: initialQuantity,
                 minimumStockLevel: 5
                 );

            var quantityToReceive = 5;
            product.Discontinue();

            // Act // Status="Discontinued"
           var act=()=>product.ReceiveQuantity( quantityToReceive );

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Cannot receive quantity for product with status {ProductStatus.Discontinued}");
        }
    }
}
