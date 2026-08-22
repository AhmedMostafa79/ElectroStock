using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Application.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using System.Data;

namespace ElectroStock.Tests.UnitTests.ApplicationLayer
{
    public class StockServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IStockMovementRepository> _mockStockMovementRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IStockService _stockService;

        public StockServiceTests()
        {
            _mockProductRepo=new Mock<IProductRepository>();
            _mockStockMovementRepo=new Mock<IStockMovementRepository>();
            _mockUnitOfWork=new Mock<IUnitOfWork>();
            _stockService = new StockService(_mockProductRepo.Object, 
                _mockStockMovementRepo.Object, _mockUnitOfWork.Object);
        }
        private QuantifiedProduct CreateTestQuantifiedProduct()
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
        public async Task ConfirmSaleAsync_WithValidData_ConfirmsSale()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();
            product.Publish();

            var performedBy=Guid.NewGuid();
            var quantity = 1;

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);

            _mockStockMovementRepo.Setup(x => x.AddAsync(It.IsAny<StockMovement>()))
                .Returns(Task.CompletedTask);

            // Act
            var confirmedSale=await _stockService.ConfirmSaleAsync(product.Id,performedBy, quantity);
            
            // Assert
            confirmedSale.ProductId.Should().Be(product.Id);
            confirmedSale.Quantity.Should().Be(quantity);
            confirmedSale.UnitPrice.Should().Be(product.UnitPrice);

            // Verify
            _mockProductRepo.Verify(x => x.Update(product),Times.Once);
            _mockStockMovementRepo.Verify(x => x.AddAsync(It.IsAny<StockMovement>()), Times.Once);
        }
        [Fact]
        public async Task ConfirmSaleAsync_WhenStockNotFound_ThrowsException()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();
            var performedBy=Guid.NewGuid();
            var quantity = 1;

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync((Product?)null);

            // Act
            var act=async ()=> await _stockService.ConfirmSaleAsync(product.Id,performedBy, quantity);


            // Assert & Verify
            await act.Should().ThrowAsync<KeyNotFoundException>();

            _mockProductRepo.Verify(x => x.Update(product),Times.Never);
            _mockStockMovementRepo.Verify(x => x.AddAsync(It.IsAny<StockMovement>()), Times.Never);
        }
        [Fact]
        public async Task ConfirmReservedSaleAsync_WithValidData_ConfirmsReservedSale()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            product.Publish();
            product.Reserve(1);

            var performedBy=Guid.NewGuid();
            var quantity = 1;

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);

            _mockStockMovementRepo.Setup(x => x.AddAsync(It.IsAny<StockMovement>()))
                .Returns(Task.CompletedTask);

            // Act
            var confirmedSale=await _stockService.ConfirmReservedSaleAsync(product.Id,performedBy, quantity);
            
            // Assert
            confirmedSale.ProductId.Should().Be(product.Id);
            confirmedSale.Quantity.Should().Be(quantity);
            confirmedSale.UnitPrice.Should().Be(product.UnitPrice);

            // Verify
            _mockProductRepo.Verify(x => x.Update(product),Times.Once);
            _mockStockMovementRepo.Verify(x => x.AddAsync(It.IsAny<StockMovement>()), Times.Once);
        }
        [Fact]
        public async Task ConfirmReservedSaleAsync_WhenStockNotFound_ThrowsException()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            var performedBy=Guid.NewGuid();
            var quantity = 1;

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync((Product?)null);

            // Act
            var act=async ()=> await _stockService.ConfirmReservedSaleAsync(product.Id,performedBy, quantity);


            // Assert & Verify
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _mockProductRepo.Verify(x => x.Update(product),Times.Never);
            _mockStockMovementRepo.Verify(x => x.AddAsync(It.IsAny<StockMovement>()), Times.Never);
        }
        [Fact]
        public async Task ReceiveSerializedProductAsync_WithValidData_CreatesProduct()
        {
            // Arrange
            var sku = "IP-IP15-256Blue";
            var data = new CreateSerializedProductDto
            {
                Brand = "Apple",
                Name = "IPhone15",
                Category = ProductCategory.Smartphone,
                Model = "A30106",
                UnitCost = 30000,
                UnitPrice = 35000,
                SerialNumber = "DXCWM4G",
                Imei = "542563993354648",
                WarrantyPeriod= WarrantyPeriod.SixMonths
            };
            var performedBy=Guid.NewGuid();

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockProductRepo.Setup(x => x.AddAsync(It.IsAny<Product>()))
                 .Returns(Task.CompletedTask);

            _mockStockMovementRepo.Setup(x => x.AddAsync(It.IsAny<StockMovement>()))
                .Returns(Task.CompletedTask);


            // Act
            var product = await _stockService.ReceiveSerializedProductAsync( sku,data,performedBy);


            // Assert
            product.Sku.Should().Be(sku);
            product.SerialNumber.Should().Be(data.SerialNumber);
            product.Imei.Should().Be(data.Imei);
            product.Brand.Should().Be(data.Brand);
            product.Name.Should().Be(data.Name);
            product.Category.Should().Be(data.Category);
            product.Model.Should().Be(data.Model);
            product.UnitCost.Should().Be(data.UnitCost);
            product.UnitPrice.Should().Be(data.UnitPrice);
            product.WarrantyPeriod.Should().Be(data.WarrantyPeriod);

            // Verify
            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x=>x.RollbackTransactionAsync(), Times.Never());

        }
        [Fact]
        public async Task ReceiveSerializedProductAsync_WhenFailsToAddProduct_ThrowsException()
        {
            // Arrange
            var sku = "IP-IP15-256Blue";
            var data = new CreateSerializedProductDto
            {
                Brand = "Apple",
                Name = "IPhone15",
                Category = ProductCategory.Smartphone,
                Model = "A30106",
                UnitCost = 30000,
                UnitPrice = 35000,
                SerialNumber = "DXCWM4G",
                Imei = "542563993354648",
                WarrantyPeriod= WarrantyPeriod.SixMonths
            };
            var performedBy=Guid.NewGuid();

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockProductRepo.Setup(x => x.AddAsync(It.IsAny<Product>()))
                 .ThrowsAsync(new Exception());

            // Act
            var act=async ()=> await _stockService.ReceiveSerializedProductAsync(sku,data,performedBy);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            // Verify
            _mockUnitOfWork.Verify(x=>x.RollbackTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Never());
            _mockStockMovementRepo.Verify(x=>x.AddAsync(It.IsAny<StockMovement>()), Times.Never());

        } 
        [Fact]
        public async Task ReceiveQuantifiedProductAsync_WithValidData_ReceivesProduct()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();
            var initialQuantity = product.QuantityOnHand;
            var quantityToReceive = 5;
            var performedBy=Guid.NewGuid();

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockProductRepo.Setup(x => x.GetBySkuAsync(product.Sku))
                 .ReturnsAsync(new List<Product> { product });

            _mockStockMovementRepo.Setup(x => x.AddAsync(It.IsAny<StockMovement>()))
                .Returns(Task.CompletedTask);


            // Act
            var productDto = await _stockService.ReceiveQuantifiedProductAsync(product.Sku, quantityToReceive, performedBy);


            // Assert & Verify
            productDto.QuantityOnHand.Should().Be(initialQuantity + quantityToReceive);
            _mockProductRepo.Verify(x=>x.Update(It.IsAny<Product>()), Times.Once());
            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(), Times.Once());
            _mockUnitOfWork.Verify(x=>x.RollbackTransactionAsync(), Times.Never());

        }
        [Fact]
        public async Task ReceiveQuantifiedProductAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();
            var quantityToReceive = 5;
            var performedBy = Guid.NewGuid();

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                 .ReturnsAsync((Product?)null);

            // Act
            var act = async ()=> await _stockService.ReceiveQuantifiedProductAsync(product.Sku, quantityToReceive, performedBy);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();

            // Verify
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once());
            _mockProductRepo.Verify(x => x.Update(It.IsAny<Product>()), Times.Never());
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never());
        }
        [Fact]
        public async Task ReserveStockAsync_WhenCanReserve_ReservesStock()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            product.Publish();

            var performedBy = Guid.NewGuid();
            var quantity = 1;

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);

            // Act
           await _stockService.ReserveStockAsync(product.Id, performedBy, quantity);


            // Assert & Verify
            _mockProductRepo.Verify(x => x.Update(product), Times.Once);
        }
        [Fact]
        public async Task ReserveStockAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var product = CreateTestSerializedProduct();

            var performedBy = Guid.NewGuid();
            var quantity = 1;

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync((Product?)null);

            // Act
            var act=async ()=> await _stockService.ReserveStockAsync(product.Id, performedBy, quantity);


            // Assert & Verify
            await act.Should().ThrowAsync<KeyNotFoundException>();

            _mockProductRepo.Verify(x => x.Update(product), Times.Never);
        }
        [Fact]
        public async Task ReleaseReservedProductAsync_WhenCanRelease_ReleasesProduct()
        {
            // Arrange
            var product = CreateTestSerializedProduct();
            product.Publish();
            product.Reserve(1);

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
                 .ReturnsAsync(product);

            // Act
           await _stockService.ReleaseReservedProductAsync(product.Id, 1);

            // Verify
            _mockProductRepo.Verify(x => x.Update(It.IsAny<Product>()), Times.Once());

        } 
        [Fact]
        public async Task ReleaseReservedProductAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var productId=Guid.NewGuid();
            _mockProductRepo.Setup(x => x.GetByIdAsync(productId))
                 .ReturnsAsync((Product?)null);

            // Act
           var act = async ()=> await _stockService.ReleaseReservedProductAsync(productId, 1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();

            // Verify
            _mockProductRepo.Verify(x => x.Update(It.IsAny<Product>()), Times.Never());
        }
        [Fact]
        public async Task GetCurrentStockAsync_WhenProductExists_ReturnsCurrentStock()
        {
            // Arrange
            var products = new List<Product>(){
                CreateTestSerializedProduct(), //reserved
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct(),
                CreateTestSerializedProduct() // sold out
            };
            foreach(var product in products)//publish all
                product.Publish();
            //products.First().Publish();
            products.First().Reserve();

            //products.Last().Publish();
            products.Last().ConfirmSale();

            var sku = products.First().Sku;
            _mockProductRepo.Setup(x => x.GetBySkuAsync(sku))
                 .ReturnsAsync(products);

            // Act
           var stockDto=await _stockService.GetCurrentStockAsync(sku);

            // Assert
            stockDto.Sku.Should().Be(sku);
            stockDto.QuantityOnHand.Should().Be(5); // 1 sold, remained 5
            stockDto.QuantityReserved.Should().Be(1);// 1 reserved
            stockDto.QuantityAvailable.Should().Be(4);// 1 sold, 1 reserved, available 4

        } 

        [Fact]
        public async Task GetCurrentStockAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var sku="";
            _mockProductRepo.Setup(x => x.GetBySkuAsync(sku))
                 .ReturnsAsync(new List<Product>());

            // Act
           var act = async ()=> await _stockService.GetCurrentStockAsync(sku);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
        [Fact]
        public async Task GetStockMovementByDateRangeAsync_WhenProductExists_ReturnsListOfStockMovements()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();
            var performedBy = Guid.NewGuid();
            int quantity = 5;
        

            var movement = new StockMovement(
                  productId: product.Id,
                  quantity: quantity,
                  quantityBefore: product.QuantityOnHand - quantity,
                  quantityAfter: product.QuantityOnHand,
                  referenceType: AuditLogType.StockReceipt,
                  performedBy: performedBy,
                  details: $"Success stock receipt for quantified product with id: {product.Id}, quantity: {quantity}" +
                  $" performed by: {performedBy}"
                  );

            var expectedStockMovements = new List<StockMovement>() {
                movement,
                movement,
                movement
            };

            _mockStockMovementRepo.Setup(x => x.GetByDateRangeAsync(product.Id,It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(expectedStockMovements);

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
             .ReturnsAsync(product);

            var start= DateTime.UtcNow;
            var end= DateTime.UtcNow.AddDays(10);

            // Act
           var actualStockMovements=await _stockService.GetStockMovementByDateRangeAsync(product.Id,start, end);

            // Assert
            actualStockMovements.Should()
               .BeEquivalentTo(expectedStockMovements.Select(s=>s.ToDto()));
        } 

        [Fact]
        public async Task GetStockMovementByDateRangeAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var productId=Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetByIdAsync(productId))
             .ReturnsAsync((Product?)null);

            // Act
           var act = async ()=> await _stockService
                            .GetStockMovementByDateRangeAsync(productId, It.IsAny<DateTime>(), It.IsAny<DateTime>());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();

            // Verify
            _mockStockMovementRepo
                .Verify(x => x.GetByDateRangeAsync(productId, It.IsAny<DateTime>(), It.IsAny<DateTime>())
                ,Times.Never);
        }
        
        [Fact]
        public async Task GetStockMovementHistoryAsync_WhenProductExists_ReturnsListOfStockMovements()
        {
            // Arrange
            var product = CreateTestQuantifiedProduct();
            var performedBy = Guid.NewGuid();
            int quantity = 5;
        

            var movement = new StockMovement(
                  productId: product.Id,
                  quantity: quantity,
                  quantityBefore: product.QuantityOnHand - quantity,
                  quantityAfter: product.QuantityOnHand,
                  referenceType: AuditLogType.StockReceipt,
                  performedBy: performedBy,
                  details: $"Success stock receipt for quantified product with id: {product.Id}, quantity: {quantity}" +
                  $" performed by: {performedBy}"
                  );

            var expectedStockMovements = new List<StockMovement>() {
                movement,
                movement,
                movement
            };

            _mockStockMovementRepo.Setup(x => x.GetByProductIdAsync(product.Id))
                .ReturnsAsync(expectedStockMovements);

            _mockProductRepo.Setup(x => x.GetByIdAsync(product.Id))
             .ReturnsAsync(product);

            // Act
           var actualStockMovements=await _stockService.GetStockMovementHistoryAsync(product.Id);

            // Assert
            actualStockMovements.Should()
               .BeEquivalentTo(expectedStockMovements.Select(s=>s.ToDto()));
        } 

        [Fact]
        public async Task GetStockMovementHistoryAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var productId=Guid.NewGuid();

            _mockProductRepo.Setup(x => x.GetByIdAsync(productId))
             .ReturnsAsync((Product?)null);

            // Act
           var act = async ()=> await _stockService
                            .GetStockMovementHistoryAsync(productId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();

            // Verify
            _mockStockMovementRepo
                .Verify(x => x.GetByProductIdAsync(productId),Times.Never);
        }
    }
}
