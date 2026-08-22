using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using ElectroStock.Infrastructure;
using ElectroStock.Tests.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectroStock.Tests.IntegrationTests
{
    [Collection("TestContainers")]
    public class StockServiceTests
    {
        private readonly TestContainersFixture _fixture;

        public StockServiceTests(TestContainersFixture fixture)
        {
            _fixture=fixture;
            _fixture.CleanDatabaseAsync().GetAwaiter().GetResult();
        }
        [Fact]
        public async Task ReceiveSerializedProductAsync_AddProduct_RecordStockReceipt()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string sku = "IP-IP15-256Blue";
            var createProductDto = new CreateSerializedProductDto
            {
                
                Brand = "Apple",
                Name = "IPhone15",
                Category = ProductCategory.Smartphone,
                Model = "A30106",
                UnitCost = 30000,
                UnitPrice = 35000,
                SerialNumber= "DXCWM4G",
                Imei= "542563993354648",
                WarrantyPeriod= WarrantyPeriod.SixMonths
            };
            var receivedBy=Guid.NewGuid();

            var beforeStock = await dbContext.Products
                .OfType<SerializedProduct>()
                .FirstOrDefaultAsync(p => p.SerialNumber == createProductDto.SerialNumber);

            // Act
            var productDto = await stockService
                .ReceiveSerializedProductAsync(sku,createProductDto, receivedBy);

            // Assert
            beforeStock.Should().BeNull();

            var afterStock = await dbContext.Products
               .OfType<SerializedProduct>()
               .FirstOrDefaultAsync(p => p.SerialNumber == createProductDto.SerialNumber);

            productDto.Should().BeEquivalentTo(afterStock.ToDto());

            // verify movement creation
            var stockMovement=await dbContext.StockMovements
                .FirstOrDefaultAsync(s=>s.ReferenceType == AuditLogType.StockReceipt);

            stockMovement.PerformedBy.Should().Be(receivedBy);
            stockMovement.Quantity.Should().Be(1);
            stockMovement.Details.Should().Contain($"{createProductDto.SerialNumber}");

        }

        [Fact]
        public async Task ReceiveQuantifiedProductAsync_IncreaseStock_RecordStockReceipt()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // quantified product creation setup
            var  data = new ProductCreationData
            {
                Sku = "OR-OFP-500mahWhite",
                Brand = "Oraimo",
                Name = "OraimoFreepods",
                Category = ProductCategory.Audio,
                Model = "OEB-E104DC",
                UnitCost = 1000,
                UnitPrice = 1200,
            };

            var quantifiedProduct= new QuantifiedProduct(
                productData: data,
                initialQuantity: 10,
                minimumStockLevel: 5
                );

            await dbContext.Products.AddAsync( quantifiedProduct );
            await dbContext.SaveChangesAsync();

            // stock receipt setup
            

            var beforeStock = await dbContext.Products
                .OfType<QuantifiedProduct>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.Sku==data.Sku);

            var quantityToReceive = 100;
            var receivedBy = Guid.NewGuid();
            
            // Act
            var stockDto = await stockService
                .ReceiveQuantifiedProductAsync(data.Sku, quantityToReceive, receivedBy);

            // Assert
            var afterStock = await dbContext.Products
                .OfType<QuantifiedProduct>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == data.Sku);

            // verify stock inclrement 
            afterStock.QuantityOnHand.Should().Be(beforeStock.QuantityOnHand + quantityToReceive);

            // verify stockMovement
            var stockMovement = await dbContext.StockMovements
                    .FirstOrDefaultAsync(s => s.ReferenceType == AuditLogType.StockReceipt);

            stockMovement.PerformedBy.Should().Be(receivedBy);
            stockMovement.Quantity.Should().Be(quantityToReceive);
            stockMovement.QuantityBefore.Should().Be(beforeStock.QuantityOnHand);
            stockMovement.QuantityAfter.Should().Be(afterStock.QuantityOnHand);
            stockMovement.ProductId.Should().Be(afterStock.Id);
        }

    }
}
