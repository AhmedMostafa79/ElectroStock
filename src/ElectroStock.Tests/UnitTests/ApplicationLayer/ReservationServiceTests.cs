using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using ElectroStock.Application.Services;
using ElectroStock.Domain.Models;
using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.ValueObjects;
using ElectroStock.Application.Mappings;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.ApplicationLayer
{
   
    public class ReservationServiceTests
    {
        private readonly Mock<IReservationRepository> _mockReservationRepo;
        private readonly Mock<ISequenceNumberGenerator> _mockNumberGenerator;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IStockService> _mockStockService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;

        private readonly IReservationService _reservationService;
        public ReservationServiceTests()
        {
            _mockReservationRepo = new Mock<IReservationRepository>();
            _mockNumberGenerator= new Mock<ISequenceNumberGenerator>();
            _mockProductService= new Mock<IProductService>();
            _mockStockService= new Mock<IStockService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserService= new Mock<IUserService>();
            _mockInvoiceService= new Mock<IInvoiceService>();
            _mockAuditLogService= new Mock<IAuditLogService>();

            _reservationService = new ReservationService(
                reservationRepo: _mockReservationRepo.Object,
                unitOfWork: _mockUnitOfWork.Object,
                numberGenerator:_mockNumberGenerator.Object,
                productService: _mockProductService.Object,
                stockService: _mockStockService.Object,
                auditLogService: _mockAuditLogService.Object,
                invoiceService: _mockInvoiceService.Object,
                userService: _mockUserService.Object
                );
        }
        private Product CreateTestProduct()
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
        private GuestUser CreateTestGuestUser()
        {
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };
            return new GuestUser(data);
        }

        private Reservation CreateTestReservation()
        {
            return new Reservation(
                reservationNumber: "RES-20250505-0001",
                productId: Guid.NewGuid(),
                customerId: Guid.NewGuid(),
                quantity: 10,
                unitPrice:100,
                source:RequestSource.InStore
                );
        }
        [Fact]
        public async Task CreateReservationAsync_WithValidData_CreatesReservation()
        {
            // Arrange
            var product = CreateTestProduct();
            var reservationNumber = "RES-20250505-0001";

            var guestUser = CreateTestGuestUser();
            var reservationDto = new CreateReservationDto
            {
                ProductId = product.Id,
                Quantity=1,
                FirstName= guestUser.FirstName,
                LastName= guestUser.LastName,
                PhoneNumber= guestUser.Phone,
                Source=RequestSource.InStore
            };


            _mockProductService.Setup(x => x.GetProductByIdAsync(reservationDto.ProductId))
                .ReturnsAsync(product.ToDto());

            _mockStockService.Setup(x => x.ReserveStockAsync(It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            _mockNumberGenerator.Setup(x=>x.GenerateReservationNumberAsync())
                .ReturnsAsync( reservationNumber );

            _mockReservationRepo.Setup(x => x.AddAsync(It.IsAny<Reservation>()))
                .Returns(Task.CompletedTask);

            _mockUserService.Setup(x => x.GetOrCreateGuestUserAsync(guestUser.FirstName, guestUser.LastName, guestUser.Phone))
                .ReturnsAsync(guestUser.ToDto());

            _mockAuditLogService
                .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                )).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);
            
            
            // Act
            var reservation =await _reservationService
                .CreateReservationAsync(reservationDto,Guid.NewGuid());

            // Assert
            reservation.ProductId.Should().Be(reservationDto.ProductId);
            reservation.ReservationNumber.Should().Be(reservationNumber);
            reservation.CustomerId.Should().Be(guestUser.Id);
            reservation.Source.Should().Be(reservationDto.Source);
            reservation.Quantity.Should().Be(reservationDto.Quantity);
            reservation.UnitPrice.Should().Be(product.UnitPrice);

            // Verify
            _mockAuditLogService
                           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           ),Times.Once);

            _mockUserService.Verify(x => x.GetOrCreateGuestUserAsync(guestUser.FirstName, guestUser.LastName, guestUser.Phone)
            , Times.Once());
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }
        
        [Fact]
        public async Task CreateReservationAsync_WithUnavailableStock_ThrowsException()
        {
            // Arrange
            var product = CreateTestProduct();
            var reservationNumber = "RES-20250505-0001";

            var reservationDto = new CreateReservationDto
            {
                ProductId = product.Id,
                Quantity = 1,
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "00123456789",
                Source = RequestSource.InStore
            };

            var guestUser = CreateTestGuestUser();

            _mockUserService.Setup(x => x.GetOrCreateGuestUserAsync(guestUser.FirstName, guestUser.LastName, guestUser.Phone))
                .ReturnsAsync(guestUser.ToDto());

            _mockProductService.Setup(x => x.GetProductByIdAsync(reservationDto.ProductId))
                .ReturnsAsync(product.ToDto());

            _mockStockService.Setup(x => x.ReserveStockAsync(It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception());//generic exceptions as exception types i StockService's concern

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);
          
            // Act
            var act =async ()=>await _reservationService
                .CreateReservationAsync(reservationDto,Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"Failed to create reservation for product with id: {reservationDto.ProductId}," +
                    $"order source: {reservationDto.Source}");

            // Verify
            _mockAuditLogService
                           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           ),Times.Once); // log on failure 

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
        [Fact]
        public async Task GetReservationByNumberAsync_WithExistingReservation_ReturnsReservation()
        {
            // Arrange
            var reservation = CreateTestReservation();

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);

            // Act
            var expectedReservation = await _reservationService
                .GetReservationByNumberAsync(reservation.ReservationNumber);

            // Assert // ensure all mapped properties are correct
            expectedReservation.ProductId.Should().Be(reservation.ProductId);
            expectedReservation.ReservationNumber.Should().Be(reservation.ReservationNumber);
            expectedReservation.CustomerId.Should().Be(reservation.CustomerId);
            expectedReservation.Source.Should().Be(reservation.Source);
            expectedReservation.Quantity.Should().Be(reservation.Quantity);
            expectedReservation.UnitPrice.Should().Be(reservation.UnitPrice);
            expectedReservation.TotalPrice.Should().Be(reservation.TotalPrice());
            expectedReservation.Status.Should().Be(reservation.Status);
            expectedReservation.CreatedAt.Should().Be(reservation.CreatedAt);
            expectedReservation.ExpiresAt.Should().Be(reservation.ExpiresAt);


        }
        [Fact]
        public async Task GetReservationByNumberAsync_WhenReservationNotFound_ThrowsException()
        {
            // Arrange
            var reservationNumber = "RES-20250505-0001";
            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservationNumber))
                .ReturnsAsync((Reservation?)null);

            // Act
            var act =async ()=> await _reservationService
                .GetReservationByNumberAsync(reservationNumber);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage($"Reservation with number {reservationNumber} not found!");

            // Verify
            _mockReservationRepo.Verify(x => x.GetByNumberAsync(reservationNumber), Times.Once);
        
        }

        [Fact]
        public async Task ConfirmReservationAsync_WhenCanConfirm_ConfirmsReservation()
        {
            // Arrange
            var reservation = CreateTestReservation();
            var saleDto = new SaleDto
            {
                ProductId = reservation.ProductId,
               Quantity= reservation.Quantity,
                UnitPrice=reservation.UnitPrice
            };

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);


            var performedBy = Guid.NewGuid();
            _mockStockService.Setup(x =>
            x.ConfirmReservedSaleAsync(reservation.ProductId, performedBy, reservation.Quantity))
                .ReturnsAsync(saleDto);

            var paymentMethod = PaymentMethod.Cash;
            var reservationDto = new CreateReservationInvoiceDto
            {
                ReservationNumber = reservation.ReservationNumber,
                CustomerId = reservation.CustomerId,
                OrderSource = reservation.Source,
                PaymentMethod = paymentMethod,
                ProductId = reservation.ProductId,
                Quantity = reservation.Quantity,
                UnitPrice = reservation.UnitPrice
            };

            var invoiceDto = new InvoiceDto
            {
                InvoiceNumber="AnyNumber",
                CustomerId=reservation.CustomerId,
                OrderSource=reservation.Source,
                PaymentMethod= paymentMethod,
            };

            _mockInvoiceService.Setup(x => x.CreateReservationInvoiceAsync(
                It.Is<CreateReservationInvoiceDto>(dto =>
            dto.ReservationNumber == reservation.ReservationNumber &&
            dto.CustomerId == reservation.CustomerId &&
            dto.ProductId == reservation.ProductId &&
            dto.Quantity == reservation.Quantity &&
            dto.UnitPrice == reservation.UnitPrice
        ),
        It.IsAny<Guid>())
            ).ReturnsAsync(invoiceDto);

            _mockAuditLogService
                           .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           )).Returns(Task.CompletedTask);
            
            

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var expectedReservation = await _reservationService
                .ConfirmReservationAsync(reservation.ReservationNumber,PaymentMethod.Cash, performedBy);

            // Assert // ensure all mapped properties are correct
            expectedReservation.ProductId.Should().Be(reservation.ProductId);
            expectedReservation.ReservationNumber.Should().Be(reservation.ReservationNumber);
            expectedReservation.Status.Should().Be(ReservationStatus.Confirmed);

            // Verify
            _mockInvoiceService.Verify(x=>x.CreateReservationInvoiceAsync(
                It.Is<CreateReservationInvoiceDto>(dto =>
            dto.ReservationNumber == reservation.ReservationNumber &&
            dto.CustomerId == reservation.CustomerId &&
            dto.ProductId == reservation.ProductId &&
            dto.Quantity == reservation.Quantity &&
            dto.UnitPrice == reservation.UnitPrice
        ),
        It.IsAny<Guid>()),
            Times.Once());

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }
        [Fact]
        public async Task ConfirmReservationAsync_WhenFailToConfirm_ThrowsException()
        {
            // Arrange
            var reservation = CreateTestReservation();

            reservation.Cancel();

            var reservationStatus = reservation.Status;

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                            .Returns(Task.CompletedTask);
            // Act
            var act =async ()=> await _reservationService
                .ConfirmReservationAsync(reservation.ReservationNumber,PaymentMethod.Cash, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>();//generic exception, as exception type is Reservation model's concern
            reservation.Status.Should().Be(reservationStatus);//status didn't change

            // Verify
            _mockAuditLogService
                           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           ),Times.Once);// called on failure only

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }  
        [Fact]
        public async Task CancelReservationAsync_WhenCanCancel_CancelsReservation()
        {
            // Arrange
            var reservation = CreateTestReservation();
            var saleDto = new SaleDto
            {
                ProductId = reservation.ProductId,
               Quantity= reservation.Quantity,
                UnitPrice=reservation.UnitPrice
            };

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);

            _mockAuditLogService
                           .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           )).Returns(Task.CompletedTask);
            
            _mockStockService.Setup(x =>
            x.ReleaseReservedProductAsync(reservation.ProductId, reservation.Quantity))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var expectedReservation = await _reservationService
                .CancelReservationAsync(reservation.ReservationNumber,Guid.NewGuid());

            // Assert 
            expectedReservation.Should().BeEquivalentTo(reservation.ToDto());

            // Verify
            _mockAuditLogService
                            .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                          It.IsAny<AuditLogType>(),
                          It.IsAny<string>(),
                          It.IsAny<bool>()
                          ),Times.Once);

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }
        [Fact]
        public async Task CancelReservationAsync_WhenFailToCancel_ThrowsException()
        {
            // Arrange
            var reservation = CreateTestReservation();

            reservation.Expire();

            var reservationStatus = reservation.Status;

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                            .Returns(Task.CompletedTask);
            // Act
            var act =async ()=> await _reservationService
                .CancelReservationAsync(reservation.ReservationNumber, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>();//generic exception, as exception type is Reservation model's concern
            reservation.Status.Should().Be(reservationStatus);//status didn't change

            // Verify
            _mockAuditLogService
                           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           ),Times.Once);// called on failure only

            _mockStockService.Verify(x =>
            x.ReleaseReservedProductAsync(reservation.ProductId, reservation.Quantity),Times.Never);
                ;
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        } 
        
        [Fact]
        public async Task ExpireReservationAsync_WhenCanExpire_ExpiresReservation()
        {
            // Arrange
            var reservation = CreateTestReservation();
            var saleDto = new SaleDto
            {
                ProductId = reservation.ProductId,
               Quantity= reservation.Quantity,
                UnitPrice=reservation.UnitPrice
            };

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);

            _mockAuditLogService
                           .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           )).Returns(Task.CompletedTask);
            
            _mockStockService.Setup(x =>
            x.ReleaseReservedProductAsync(reservation.ProductId, reservation.Quantity))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var expectedReservation = await _reservationService
                .ExpireReservationAsync(reservation.ReservationNumber,Guid.NewGuid());

            // Assert 
            expectedReservation.Should().BeEquivalentTo(reservation.ToDto());

            // Verify
            _mockAuditLogService
                            .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                          It.IsAny<AuditLogType>(),
                          It.IsAny<string>(),
                          It.IsAny<bool>()
                          ),Times.Once);//On Success

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }
        [Fact]
        public async Task ExpireReservationAsync_WhenFailToExpire_ThrowsException()
        {
            // Arrange
            var reservation = CreateTestReservation();

            reservation.Expire();

            var reservationStatus = reservation.Status;

            _mockReservationRepo.Setup(x => x.GetByNumberAsync(reservation.ReservationNumber))
                .ReturnsAsync(reservation);

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                            .Returns(Task.CompletedTask);
            // Act
            var act =async ()=> await _reservationService
                .ExpireReservationAsync(reservation.ReservationNumber, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>();//generic exception, as exception type is Reservation model's concern
            reservation.Status.Should().Be(reservationStatus);//status didn't change

            // Verify
            _mockAuditLogService
                           .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                           It.IsAny<AuditLogType>(),
                           It.IsAny<string>(),
                           It.IsAny<bool>()
                           ),Times.Once);// called on failure only

            _mockStockService.Verify(x =>
            x.ReleaseReservedProductAsync(reservation.ProductId, reservation.Quantity),Times.Never);
                ;
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        } 
       
    }
}
