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
    public class ProductServiceTests
    {
        private readonly TestContainersFixture _fixture;
        public ProductServiceTests(TestContainersFixture fixture)
        {
            _fixture = fixture;
            _fixture.CleanDatabaseAsync().GetAwaiter().GetResult();
        }
        [Fact]
        public async Task CreateQuantifiedProductAsync_CreateProduct_RecordStockMovement_LogAudit()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var createProductDto = new CreateQuantifiedProductDto
            {
                Sku = "OR-OFP-500mahWhite",
                Brand = "Oraimo",
                Name = "OraimoFreepods",
                Category = ProductCategory.Audio,
                Model = "OEB-E104DC",
                UnitCost = 1000,
                UnitPrice = 1200,
                InitialQuantity = 10,
                MinimumStockLevel=5
            };
            var createdAndReceivedBy = Guid.NewGuid();

            // Act
            var productDto = await productService
                .CreateQuantifiedProductAsync(createProductDto, createdAndReceivedBy);

            // Assert
            
            // verify product added successfully
            var createdProduct = await dbContext.Products
                .OfType<QuantifiedProduct>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productDto.Id);

            productDto.Should().BeEquivalentTo(createdProduct.ToDto());

            // verify stockMovement recorded 
            var stockMovement = await dbContext.StockMovements
                   .FirstOrDefaultAsync(s => s.ReferenceType == AuditLogType.StockReceipt);

            stockMovement.PerformedBy.Should().Be(createdAndReceivedBy);
            stockMovement.Quantity.Should().Be(createProductDto.InitialQuantity);
            stockMovement.QuantityBefore.Should().Be(0);
            stockMovement.QuantityAfter.Should().Be(createProductDto.InitialQuantity);
            stockMovement.ProductId.Should().Be(createdProduct.Id);

            // verify product creation auditLog
            var auditLog = await dbContext.AuditLogs
                .FirstOrDefaultAsync(a=>a.ReferenceType==AuditLogType.ProductCreation);

            auditLog.PerformedBy.Should().Be(createdAndReceivedBy);
            auditLog.Details.Should().Contain($"{createdProduct.Id}");
        }
        [Fact]
        public async Task PublishProductAsync_PublishesProduct_RecordAuditLog()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();


            // product setup
            var productData = new ProductCreationData
            {
                Sku = "IP-IP15-256Blue",
                Brand = "Apple",
                Name = "IPhone15",
                Category = ProductCategory.Smartphone,
                Model = "A30106",
                UnitCost = 30000,
                UnitPrice = 35000,
              
            };
            var serialNumber = "DXCWM4G";
            var imei = "542563993354648";
            var warrantyPeriod = WarrantyPeriod.SixMonths;
            var product = new SerializedProduct(productData,
                serialNumber,
                imei,
                warrantyPeriod
                );
            await dbContext.Products
                .AddAsync( product );
            await dbContext.SaveChangesAsync();


            var updatedBy=Guid.NewGuid();
            // i could user "product" as is still being tracking
            // but used this so that it is not tracked
            var beforeProduct = await dbContext.Products
                  .OfType<SerializedProduct>()
                  .AsNoTracking()
                  .FirstOrDefaultAsync(p => p.Id ==product.Id);
            
            // Act
            await productService.PublishProductAsync(beforeProduct.Id, updatedBy);

            // Assert
            var afterProduct = await dbContext.Products
                  .OfType<SerializedProduct>()
                  .AsNoTracking()
                  .FirstOrDefaultAsync(p => p.Id == product.Id);

            // verify product status
            beforeProduct.Status.Should().Be(ProductStatus.Draft);
            afterProduct.Status.Should().Be(ProductStatus.Active);

            // verify AuditLog record
            var auditLog = await dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.ReferenceType == AuditLogType.ProductUpdate);

            auditLog.PerformedBy.Should().Be(updatedBy);
            auditLog.Details.Should().Contain($"{product.Id}");
        }
    }
}
