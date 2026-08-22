using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class ProductTests
    {
        private Product CreateTestQuantifiedProduct()
        {
            var data = new ProductCreationData
            {
                Sku = "OR-OFP-500mahWhite",
                Brand = "Oraimo",
                Name = "OraimoFreepods",
                Category = ProductCategory.Audio,
                Model = "OEB-E104DC",
                UnitCost = 1000,
                UnitPrice = 1200,
            };

            return  new QuantifiedProduct(
                productData: data,
                initialQuantity: 10,
                minimumStockLevel: 5
                );
        }
        private SerializedProduct CreateTestSerializedProduct()
        {
            var data = new ProductCreationData
            {
                Sku = "IP-IP15-256Blue",
                Brand = "Apple",
                Name = "IPhone15",
                Category = ProductCategory.Smartphone,
                Model = "A30106",
                UnitCost = 30000,
                UnitPrice = 35000,
            };

            string serialNumber = "DXCWM4G";
            string imei = "542563993354648";
            var warrantyPeriod = WarrantyPeriod.SixMonths;

            return new SerializedProduct(
                productData: data,
                serialNumber: serialNumber,

                imei: imei,
                warrantyPeriod: warrantyPeriod
                );
        }
        [Fact]
        public void Constructor_WithValidData_CreatesProduct()
        {

            // Arrange
            var data = new ProductCreationData {
                Sku="OR-OFP-500mahWhite",
                Brand="Oraimo",
                Name="OraimoFreepods",
                Category=ProductCategory.Audio,
                Model="OEB-E104DC",
                UnitCost=1000,
                UnitPrice=1200,
                };

            // Act
            var product = new QuantifiedProduct(
                productData:data,
                initialQuantity:10,
                minimumStockLevel:5
                );

            // Assert
            product.Sku.Should().Be(data.Sku);
            product.Brand.Should().Be(data.Brand);
            product.Name.Should().Be(data.Name);
            product.Category.Should().Be(data.Category);
            product.Model.Should().Be(data.Model);
            product.UnitCost.Should().Be(data.UnitCost);
            product.UnitPrice.Should().Be(data.UnitPrice);
            product.ProductType.Should().Be(ProductType.Quantified);
            product.Status.Should().Be(ProductStatus.Draft);
        } 
        [Theory]
        [InlineData("aa-bb-cc")]
        [InlineData("aa-b2-c3")]
        [InlineData("bb-b2b-33c")]
        [InlineData("xxxxx-22-33")]
        [InlineData("abcd-ab-ab")]
        public void Constructor_WithValidSku_CreatesProduct(string sku)
        {

            // Arrange
            var data = new ProductCreationData {
                Sku= sku,
                Brand="Oraimo",
                Name="OraimoFreepods",
                Category=ProductCategory.Audio,
                Model="OEB-E104DC",
                UnitCost=1000,
                UnitPrice=1200,
                };

            // Act
            var product = new QuantifiedProduct(
                productData:data,
                initialQuantity:10,
                minimumStockLevel:5
                );

            // Assert
            product.Sku.Should().Be(sku);
        } 
        
        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData(" - - ")]
        [InlineData("asdasdsa")]
        [InlineData("a-b-c")]
        [InlineData("aa-b")]
        [InlineData("aa-b-")]
        [InlineData("-aa-b-")]
        [InlineData("a1-b2-c3")]
        [InlineData("1a-2b-3c")]
        [InlineData("11-22-33")]
        public void Constructor_WithInvalidSku_ThrowsException(string sku)
        {

            // Arrange
            var data = new ProductCreationData {
                Sku= sku,
                Brand="Oraimo",
                Name="OraimoFreepods",
                Category=ProductCategory.Audio,
                Model="OEB-E104DC",
                UnitCost=1000,
                UnitPrice=1200,
                };

            // Act
            var act = ()=>new QuantifiedProduct(
                productData:data,
                initialQuantity:10,
                minimumStockLevel:5
                );

            // Assert
           act.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid Sku: {sku}!");
        } 
        [Theory]
        [InlineData(1000,999)]
        [InlineData(1000,999.9999999)]
        public void ChangePrice_WithPriceSmallerThanCost_ThrowsException(decimal unitCost, decimal newPrice)
        {

            // Arrange
            var initialPrice = unitCost + 100;
            var data = new ProductCreationData {
                Sku="OR-OFP-500mahWhite",
                Brand="Oraimo",
                Name="OraimoFreepods",
                Category=ProductCategory.Audio,
                Model="OEB-E104DC",
                UnitCost=unitCost,
                UnitPrice= initialPrice,
                };
            var product= new QuantifiedProduct(
                productData: data,
                initialQuantity: 10,
                minimumStockLevel: 5
                );

            // Act
            var act = () => product.ChangePrice(newPrice);

            // Assert
           act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Unit Price can't be less than Unit Cost, " +
               $"Price to change {newPrice}, Unit Cost {product.UnitCost}");
        }
        [Theory]
        [InlineData(1000,999)]
        [InlineData(1000,999.9999999)]
        public void ChangeCost_WithCostGreaterThanPrice_ThrowsException(decimal newCost, decimal unitPrice)
        {

            var initialCost = unitPrice-1;
            // Arrange
            var data = new ProductCreationData {
                Sku="OR-OFP-500mahWhite",
                Brand="Oraimo",
                Name="OraimoFreepods",
                Category=ProductCategory.Audio,
                Model="OEB-E104DC",
                UnitCost= initialCost,
                UnitPrice= unitPrice,
                };
            var product= new QuantifiedProduct(
                productData: data,
                initialQuantity: 10,
                minimumStockLevel: 5
                );

            // Act
            var act = () => product.ChangeCost(newCost);

            // Assert
           act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Unit Price can't be less than Unit Cost, " +
                $"Unit Price {product.UnitPrice}, Cost to change{newCost}");
        }
        [Fact]
        public void Discontinue_WhenNotSoldOut_ChangesStatusToDiscontinue()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();


            // Act
            product.Discontinue();

            // Assert
            product.Status.Should().Be(ProductStatus.Discontinued);
        } 
        [Fact]
        public void Discontinue_WhenSoldOut_ThrowsException()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            product.Publish();

            product.ConfirmSale();

            // Act

            var act=()=>product.Discontinue();

            // Assert
            act.Should()
                .Throw<InvalidOperationException>();

        } 
       
    }
}
