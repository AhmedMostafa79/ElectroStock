using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Interfaces.Repositories;
using Moq;
using ElectroStock.Application.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Mappings;
using FluentAssertions;
using System.Data;

namespace ElectroStock.Tests.UnitTests.ApplicationLayer
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IStockService> _mockStockService;

        private readonly IProductService _productService;
        public ProductServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProductRepo= new Mock<IProductRepository>();
            _mockAuditLogService= new Mock<IAuditLogService>();
            _mockStockService = new Mock<IStockService>();
            _productService = new ProductService(
                unitOfWork: _mockUnitOfWork.Object,
                productRepo: _mockProductRepo.Object,
                auditLogService: _mockAuditLogService.Object,
                stockService: _mockStockService.Object
                );
        }
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

            return new QuantifiedProduct(
                productData: data,
                initialQuantity: 10,
                minimumStockLevel: 5
                );
        } 
        private Product CreateTestSerializedProduct()
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
        public async Task CreateQuantifiedProductAsync_WithValidData_CreatesProduct()
        {
            // Arrange
            var productDto = new CreateQuantifiedProductDto
            {
                Sku = "OR-OFP-500mahWhite",
                Brand = "Oraimo",
                Name = "OraimoFreepods",
                Category = ProductCategory.Audio,
                Model = "OEB-E104DC",
                UnitCost = 1000,
                UnitPrice = 1200,
                InitialQuantity=10,
                MinimumStockLevel=10

            };

            _mockProductRepo.Setup(x => x.AddAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            _mockAuditLogService
                .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x=>x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var actualProduct = await _productService.CreateQuantifiedProductAsync(productDto, Guid.NewGuid());


            // Assert
            actualProduct.Sku.Should().Be(productDto.Sku);
            actualProduct.Brand.Should().Be(productDto.Brand);
            actualProduct.Name.Should().Be(productDto.Name);
            actualProduct.Category.Should().Be(productDto.Category);
            actualProduct.Model.Should().Be(productDto.Model);
            actualProduct.UnitCost.Should().Be(productDto.UnitCost);
            actualProduct.UnitPrice.Should().Be(productDto.UnitPrice);

            // Verify
            _mockAuditLogService
              .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              ),Times.Once);// On-Success

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        } 
        [Fact]
        public async Task CreateQuantifiedProductAsync_WhenFailedToCommit_RollsBack()
        {
            // Arrange
            var productDto = new CreateQuantifiedProductDto
            {
                Sku = "OR-OFP-500mahWhite",
                Brand = "Oraimo",
                Name = "OraimoFreepods",
                Category = ProductCategory.Audio,
                Model = "OEB-E104DC",
                UnitCost = 1000,
                UnitPrice = 1200,
                InitialQuantity=10,
                MinimumStockLevel=10

            };

            var expectedProduct = new QuantifiedProduct(
                productData: productDto.ToCreationData(),
                initialQuantity: 10,
                minimumStockLevel: 5
                );

            _mockProductRepo.Setup(x => x.AddAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            _mockAuditLogService
                .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x=>x.CommitTransactionAsync())
                .Throws(new Exception());

            // Act
            var act = async ()=>await _productService.CreateQuantifiedProductAsync(productDto, Guid.NewGuid());


            // Assert
            await act.Should().ThrowAsync<Exception>(); //error comes from UnitOfWork, so exception type is UoW concern
               ;

            // Verify
            _mockAuditLogService
              .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              ),Times.Exactly(2));// 1 On-Success then rolls back,1 On-Failure

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
        [Fact]
        public async Task GetProductByIdAsync_WhenProductExists_ReturnsProduct()
        {
            // Arrange
            var expectedProduct = CreateTestQuantifiedProduct();

            _mockProductRepo.Setup(x=>x.GetByIdAsync(expectedProduct.Id))
                .ReturnsAsync(expectedProduct);


            // Act
            var actualProduct = await _productService.GetProductByIdAsync(expectedProduct.Id);

            // Assert
            actualProduct.Should().BeEquivalentTo(expectedProduct.ToDto());
        }
        [Fact]
        public async Task GetProductByIdAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var expectedProduct = CreateTestQuantifiedProduct();

            _mockProductRepo.Setup(x=>x.GetByIdAsync(expectedProduct.Id))
                .ReturnsAsync((Product?)null);


            // Act
            var act = async ()=>await _productService.GetProductByIdAsync(expectedProduct.Id);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
        [Fact]
        public async Task GetProductsBySkuAsync_ReturnsListOfMatchingProducts()
        {
            // Arrange
            
            var expectedProducts = new List<Product>
            {
                CreateTestQuantifiedProduct(),
                CreateTestQuantifiedProduct(),
                CreateTestQuantifiedProduct()
            };
            
            var sku= expectedProducts.First().Sku;

            _mockProductRepo.Setup(x=>x.GetBySkuAsync(sku))
                .ReturnsAsync(expectedProducts);


            // Act
            var actualProducts = await _productService.GetProductsBySkuAsync(sku);

            // Assert
            var actualProductsDtos = expectedProducts.Select(p => p.ToDto());

            actualProducts.Should().BeEquivalentTo(actualProductsDtos);
        }
        [Fact]
        public async Task GetProductsByCategoryAsync_ReturnsListOfMatchingProducts()
        {
            // Arrange
            
            var expectedProducts = new List<Product>
            {
                CreateTestQuantifiedProduct(),
                CreateTestQuantifiedProduct(),
                CreateTestQuantifiedProduct()
            };
            
            var category= expectedProducts.First().Category;

            _mockProductRepo.Setup(x=>x.GetByCategoryAsync(category))
                .ReturnsAsync(expectedProducts);


            // Act
            var actualProducts = await _productService.GetProductsByCategoryAsync(category);

            // Assert
            var actualProductsDtos = expectedProducts.Select(p => p.ToDto());

            actualProducts.Should().BeEquivalentTo(actualProductsDtos);
        } 
        [Fact]
        public async Task GetAllProductsAsync_ReturnsListOfAllProducts()
        {
            // Arrange
            
            var expectedProducts = new List<Product>
            {
                CreateTestQuantifiedProduct(),
                CreateTestQuantifiedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct()
            };

            _mockProductRepo.Setup(x => x.GetAllAsync())
               .ReturnsAsync(expectedProducts);

            // Act
            var actualProducts = await _productService.GetAllProductsAsync();

            // Assert
            var actualProductsDtos = expectedProducts.Select(p => p.ToDto());

            actualProducts.Should().BeEquivalentTo(actualProductsDtos);
        } 
        
        [Fact]
        public async Task UpdateProductsPriceAsync_WithValidData_UpdatesPrice()
        {
            // Arrange
            var products = new List<Product>(){
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct()
            };
            var newPrice = products.First().UnitPrice + 1000;
            var performedBy = Guid.NewGuid();

            var sku = products.First().Sku;
            _mockProductRepo.Setup(x => x.GetBySkuAsync(sku))
               .ReturnsAsync(products);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _productService.UpdateProductsPriceAsync(sku, newPrice, performedBy);

            // Assert
            foreach (var product in products)
                product.UnitPrice.Should().Be(newPrice);

            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Once());
        } 
        [Fact]
        public async Task UpdateProductsPriceAsync_WhenFailsToCommitTransaction_ThrowsException()
        {
            // Arrange
            var products = new List<Product>(){
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct()
            };
            var initialPrice = products.First().UnitPrice;
            var newPrice = initialPrice + 1000;

            var performedBy = Guid.NewGuid();
            var sku = products.First().Sku;

            _mockProductRepo.Setup(x => x.GetBySkuAsync(sku))
               .ReturnsAsync(products);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .ThrowsAsync(new Exception());

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var act=async ()=>await _productService.UpdateProductsPriceAsync(sku, newPrice, performedBy);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x=>x.RollbackTransactionAsync(), Times.Once());

            _mockAuditLogService
              .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              ),Times.Exactly(2));
        }
        
        [Fact]
        public async Task UpdateProductsCostAsync_WithValidData_UpdatesCost()
        {
            // Arrange
            var products = new List<Product>(){
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
            };

            var newCost = products.First().UnitCost + 1000;

            var performedBy = Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetBySkuAsync(products.First().Sku))
               .ReturnsAsync(products);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _productService.UpdateProductsCostAsync(products.First().Sku, newCost, performedBy);

            // Assert
            foreach (var product in products)
                product.UnitCost.Should().Be(newCost);

            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Once());
        } 
        [Fact]
        public async Task UpdateProductsCostAsync_WhenFailsToCommitTransaction_ThrowsException()
        {
            // Arrange
            var products = new List<Product>(){
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct()
            };
            var newCost = products.First().UnitCost+ 1000;
            var performedBy = Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetBySkuAsync(products.First().Sku))
               .ReturnsAsync(products);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .ThrowsAsync(new Exception());

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var act=async ()=>await _productService.UpdateProductsCostAsync(products.First().Sku, newCost, performedBy);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x=>x.RollbackTransactionAsync(), Times.Once());

            _mockAuditLogService
              .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              ),Times.Exactly(2));
        }

        [Fact]
        public async Task DiscontinueProductAsync_WhenCanDiscontinue_DiscontinuesProduct()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            var performedBy = Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
              .ReturnsAsync(product);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _productService.DiscontinueProductAsync(product.Id, performedBy);

            // Assert
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once());

            _mockAuditLogService
           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
           It.IsAny<AuditLogType>(),
           It.IsAny<string>(),
           It.IsAny<bool>()
           ), Times.Once);

        } 
        [Fact]
        public async Task DiscontinueProductAsync_WhenFailsToDiscontinue_ThrowsException()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            var performedBy = Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
              .ReturnsAsync(product);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .ThrowsAsync(new Exception());

            // Act
            var act=async ()=>await _productService.DiscontinueProductAsync(product.Id, performedBy);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once());

            _mockAuditLogService
           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
           It.IsAny<AuditLogType>(),
           It.IsAny<string>(),
           It.IsAny<bool>()
           ), Times.Exactly(2));

        } 
        
        [Fact]
        public async Task PublishProductAsync_WhenCanPublish_PublishesProduct()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            var performedBy = Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
              .ReturnsAsync(product);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _productService.PublishProductAsync(product.Id, performedBy);

            // Assert
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once());

            _mockAuditLogService
           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
           It.IsAny<AuditLogType>(),
           It.IsAny<string>(),
           It.IsAny<bool>()
           ), Times.Once);

        } 
        [Fact]
        public async Task PublishProductAsync_WhenFailsToPublish_ThrowsException()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            var performedBy = Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
              .ReturnsAsync(product);

            _mockAuditLogService
              .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
              It.IsAny<AuditLogType>(),
              It.IsAny<string>(),
              It.IsAny<bool>()
              )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .ThrowsAsync(new Exception());

            // Act
            var act=async ()=>await _productService.PublishProductAsync(product.Id, performedBy);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once());

            _mockAuditLogService
           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
           It.IsAny<AuditLogType>(),
           It.IsAny<string>(),
           It.IsAny<bool>()
           ), Times.Exactly(2));

        }

    }
}
