using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class SerializedProductTests
    {
        private ProductCreationData CreateTestData()
        {
            return new ProductCreationData
            {
                Sku = "IP-IP15-256Blue",
                Brand = "Apple",
                Name = "IPhone15",
                Category = ProductCategory.Smartphone,
                Model = "A30106",   
                UnitCost = 30000,
                UnitPrice = 35000,
            };
        }
        [Fact]
        public void Constructor_WithValidData_CreatesProduct()
        {
            // Arrange
            var data = CreateTestData();
            string serialNumber = "DXCWM4G";
            string imei = "542563993354648";
            var warrantyPeriod = WarrantyPeriod.SixMonths;

            // Act
            var product = new SerializedProduct(data, serialNumber, imei, warrantyPeriod);

            // Assert // validating only Serialized Product related properties
            product.SerialNumber.Should().Be(serialNumber);
            product.Imei.Should().Be(imei);
            product.WarrantyPeriod.Should().Be(warrantyPeriod);
            product.SoldDate.Should().BeNull();
            product.ProductType.Should().Be(ProductType.Serialized);
        }
        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData(null)]
        [InlineData("54256399335464")]//14 digits
        [InlineData(".542563993354648")]//15 digits and contains "." (symbol)
        [InlineData("5425639933546 48")] // 15 digits, and a space
        [InlineData("A54256399335464")]  // 14 digits, a letter "A"
        [InlineData("542-563-993-354-648")]  // contains separators
        public void Constructor_WithInvalidLengthOrNonDigits_ThrowsException(string imei)
        {
            // Arrange
            var data = CreateTestData();
            string serialNumber = "DXCWM4G";
            var warrantyPeriod = WarrantyPeriod.SixMonths;

            // Act
            var act = ()=>new SerializedProduct(data, serialNumber, imei, warrantyPeriod);

            // Assert 
          act.Should().Throw<ArgumentException>()
                .WithMessage($"IMEI must consist of exactly 15 digits {imei}");
        }
        
        [Theory]
        [InlineData("123456789012346")]
        [InlineData("123456789012348")]
        [InlineData("123456789012340")]
        public void Constructor_WithInvalidLuhnCheckSum_ThrowsException(string imei)
        {
            // Arrange
            var data = CreateTestData();
            string serialNumber = "DXCWM4G";
            var warrantyPeriod = WarrantyPeriod.SixMonths;

            // Act
            var act =()=> new SerializedProduct(data, serialNumber, imei, warrantyPeriod);

            // Assert 
            act.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid IMEI checksum {imei}");
        }
        
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        [InlineData("ABC4")]
        [InlineData("542563993354648542563993354648542563993354648542563993354648")]
        public void Constructor_WithInvalidSerialNumber_ThrowsException(string serialNumber)
        {
            // Arrange
            var data = CreateTestData();
            var warrantyPeriod = WarrantyPeriod.SixMonths;
            
            // Act 
            var act = ()=> new SerializedProduct(data, serialNumber, imei: "542563993354648", warrantyPeriod);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ConfirmSale_WithValidQuantityAndStatus_ConfirmsSale()
        {
            // Arrange
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);

            // Act & Assert
            product.CanReserveOrSell().Should().BeFalse();//Status="Draft"
            product.Publish();//activate
            
            product.CanReserveOrSell().Should().BeTrue();
            product.ConfirmSale(1);

            product.Status.Should().Be(ProductStatus.SoldOut);
            product.SoldDate.Should().NotBeNull();

            product.CanReserveOrSell().Should().BeFalse();
        }
        
        [Fact]
        public void ConfirmSale_WithInvalidQuantityOrStatus_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);

            // Act & Assert
            product.Status.Should().Be(ProductStatus.Draft);
            product.CanReserveOrSell().Should().BeFalse();

            var act=()=>product.ConfirmSale(); //ProductStatus="Draft" 

            act.Should().Throw<InvalidOperationException>() //throws due to status="draft"
                .WithMessage($"Item is unavailable or insufficient quantity");

            product.Publish();
            product.Status.Should().Be(ProductStatus.Active);

            var act2=()=>product.ConfirmSale(5);
            act2.Should().Throw<InvalidOperationException>() //throws due to quantity!=1
               .WithMessage($"Item is unavailable or insufficient quantity");
        }
        [Fact]
        public void ConfirmReservedSale_WhenProductReserved_ConfirmsSale()
        {
            // Arrange
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);
            product.Publish();
            product.Reserve(1);

            // Act 
            product.ConfirmReservedSale(1);

            // Assert
            product.Status.Should().Be(ProductStatus.SoldOut);
            product.SoldDate.Should().NotBeNull();
            product.CanReserveOrSell(1).Should().BeFalse();
        }
        
        [Fact]
        public void ConfirmReservedSale_WhenNotReserved_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);
            product.Publish();// status="Draft"
            var quantity = 5;
            // Act 

            var act=()=>product.ConfirmReservedSale(quantity); //ProductStatus="Draft" 

            act.Should().Throw<InvalidOperationException>() //throws due to quantity!=1
                .WithMessage($"Insufficient quantity {quantity}");


            var act2=()=>product.ConfirmReservedSale(1);
            act2.Should().Throw<InvalidOperationException>() //throws due to status="Draft"
               .WithMessage($"Product is not reserved! or is Sold out, product status {product.Status}");
        }

        [Fact]
        public void Reserve_WithValidQuantityAndStatus_ReservesItem()
        {
            // Arrange
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);

            // Act & Assert
            product.Publish();//activate

            product.CanReserveOrSell().Should().BeTrue();
            product.Reserve(1);

            product.Status.Should().Be(ProductStatus.Reserved);

        }
        [Fact]
        public void Reserve_WhenAlreadyReserved_ThrowsException()
        {
            // Arrange
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);

            product.Publish();
            // Act & Assert

            product.CanReserveOrSell().Should().BeTrue();
            product.Reserve(1);
            product.CanReserveOrSell().Should().BeFalse();

           var act=()=> product.Reserve(1);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Item is already reserved");

            product.Status.Should().Be(ProductStatus.Reserved);
        }

        [Fact]
        public void ReleaseReservation_WhenReserved_ReleasesReservation()
        {
            // Arrange 
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);
            product.Publish();
            // Act & Assert
            product.Reserve(1);
            product.Status.Should().Be(ProductStatus.Reserved);

            product.ReleaseReservation(1);
            product.Status.Should().Be(ProductStatus.Active);

        }
        [Fact]
        public void ReleaseReservation_WhenNotReserved_ThrowsException()
        {
            // Arrange 
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);

            // Act & Assert

            product.Status.Should().Be(ProductStatus.Draft);

            var act=()=> product.ReleaseReservation(1);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Item is not reserved. Current status: {product.Status}");
            product.Status.Should().Be(ProductStatus.Draft);

        }
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(2)]
        public void ReleaseReservation_WithInvalidQuantity_ThrowsException(int quantity)
        {
            // Arrange 
            var data = CreateTestData();
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);

            // Act & Assert

            product.Status.Should().Be(ProductStatus.Draft);

            var act=()=> product.ReleaseReservation(quantity);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Insufficient quantity {quantity}");

            product.Status.Should().Be(ProductStatus.Draft);

        }

        [Fact]
        public void IsUnderWarranty_ReturnsTrueIfCurrentTimeUnderWarranty()
        {
            // Arrange 
            var data = CreateTestData();

            // Act & Assert // Under Warranty
            var product = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.SixMonths);
            product.Publish();

            product.IsUnderWarranty().Should().BeFalse();// not sold yet so false
            product.ConfirmSale(1);
            product.IsUnderWarranty().Should().BeTrue();// not sold yet so false
           
            // Act & Assert 2 //No Warranty
            var product2 = new SerializedProduct(data, "DXCWM4G", "542563993354648", WarrantyPeriod.None);//no warranty
            product2.Publish();
            product2.ConfirmSale(1);
            product2.IsUnderWarranty().Should().BeFalse();


        }
    }
}
