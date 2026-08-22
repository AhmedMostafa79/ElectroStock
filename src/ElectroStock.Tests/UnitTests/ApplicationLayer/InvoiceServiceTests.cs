using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Application.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using ElectroStock.Infrastructure.Repositories;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.UnitTests.ApplicationLayer
{
    public class InvoiceServiceTests
    {
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
        private readonly Mock<IStockService> _mockStockService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ISequenceNumberGenerator> _mockNumberGenerator;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IInvoiceService _invoiceService;
        public InvoiceServiceTests()
        {
            _mockAuditLogService= new Mock<IAuditLogService>();
            _mockInvoiceRepo= new Mock<IInvoiceRepository>();
            _mockStockService= new Mock<IStockService>();
            _mockUserService= new Mock<IUserService>();
            _mockNumberGenerator= new Mock<ISequenceNumberGenerator>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _invoiceService = new InvoiceService(
                auditLogService: _mockAuditLogService.Object,
                invoiceRepo: _mockInvoiceRepo.Object,
                stockService:_mockStockService.Object,
                numberGenerator: _mockNumberGenerator.Object,
                unitOfWork:_mockUnitOfWork.Object,
                userService:_mockUserService.Object
                );
        }

        [Fact]
        public async Task CreateInvoiceAsync_WithValidData_CreatesInvoice()
        {
            // Arrange
            var invoiceNumber = "INV-20250505-0001";

            var itemsToAdd = new List<AddInvoiceItemDto> {
            new AddInvoiceItemDto{ ProductId=Guid.NewGuid(),Quantity=10},
                new AddInvoiceItemDto{ ProductId=Guid.NewGuid(),Quantity=100},
                new AddInvoiceItemDto{ ProductId=Guid.NewGuid(),Quantity=999}
            };

            var invoiceDto = new CreateInvoiceDto
            {
                FirstName="Ahmed",
                LastName="User",
                PhoneNumber="01234567890",
                OrderSource= RequestSource.InStore,
                PaymentMethod= PaymentMethod.Cash,
                // mapping InvoiceItem into CreateInvoiceDto
                Items= itemsToAdd
            };

            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                FirstName = invoiceDto.FirstName,
                LastName = invoiceDto.LastName,
                PhoneNumber = invoiceDto.PhoneNumber,
                Email = null,
                UserStatus = null,
                Role = UserRole.Customer,
                UserType = UserType.Registered,
                CreatedAt = DateTime.UtcNow
            };

            // customer mocking
            _mockUserService
                .Setup(x => 
                x.GetOrCreateGuestUserAsync(invoiceDto.FirstName, invoiceDto.LastName, invoiceDto.PhoneNumber))
                .ReturnsAsync(userDto);

            //numberGenerator mocking
            _mockNumberGenerator.Setup(x => x.GenerateInvoiceNumberAsync())
                .ReturnsAsync(invoiceNumber);


            //mocking stockService
            _mockStockService.Setup(x =>
            x.ConfirmSaleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .ReturnsAsync((Guid productId, Guid performedBy, int quantity)
                => new SaleDto { 
                ProductId=productId,
                Quantity=quantity,
                UnitPrice=100 //whatever price, price is StockService's concern
                });

            //mocking InvoiceRepo
            _mockInvoiceRepo
                .Setup(x => x.AddAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            // mocking auditLogService
            _mockAuditLogService
                .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                )).Returns(Task.CompletedTask);

            // mock UnitOfWork(transactions)
            _mockUnitOfWork.Setup(x=>x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);
            
            _mockUnitOfWork.Setup(x=>x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act

            var invoice = await _invoiceService.CreateInvoiceAsync(invoiceDto, Guid.NewGuid());

            // Assert

            //invoice.
            invoice.InvoiceNumber.Should().BeEquivalentTo(invoiceNumber);
            invoice.OrderSource.Should().Be(invoiceDto.OrderSource);
            invoice.PaymentMethod.Should().Be(invoiceDto.PaymentMethod);
            invoice.CustomerId.Should().Be(userDto.Id);
            invoice.Items.Should().HaveCount(itemsToAdd.Count);

            // verify invoiceItems (input) are invoice.Items
            var expectedItems = itemsToAdd.Select(dto => new { dto.ProductId, dto.Quantity });
            var actualItems = itemsToAdd.Select(item => new { item.ProductId, item.Quantity });
            expectedItems.Should().BeEquivalentTo(actualItems);


            // ensure ConfirmSale invoked number of items Invoice items
            _mockStockService.Verify(x =>
            x.ConfirmSaleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Exactly(itemsToAdd.Count));

            //ensure invoice is added to repo
            _mockInvoiceRepo.Verify(x => x.AddAsync(It.IsAny<Invoice>()), Times.Once);

            // ensure auditLog is invoked 
            _mockAuditLogService.Verify(x => x.LogAuditAsync(
                It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                ), Times.Once);

            // ensure CommitTransaction is called 
            _mockUnitOfWork.Verify(x=>x.CommitTransactionAsync(),Times.Once());
        }

        [Fact]
        public async Task CreateInvoiceAsync_WhenThrowsException_RollsBackTransaction()
        {
            // Arrange
            var invoiceNumber = "INV-20250505-0001";

            var itemsToAdd = new List<AddInvoiceItemDto> {
            new AddInvoiceItemDto{ ProductId=Guid.NewGuid(),Quantity=10},
                new AddInvoiceItemDto{ ProductId=Guid.NewGuid(),Quantity=100},
                new AddInvoiceItemDto{ ProductId=Guid.NewGuid(),Quantity=999}
            };

            var invoiceDto = new CreateInvoiceDto
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                OrderSource = RequestSource.InStore,
                PaymentMethod = PaymentMethod.Cash,
                Items = itemsToAdd
            };

            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                FirstName = invoiceDto.FirstName,
                LastName = invoiceDto.LastName,
                PhoneNumber = invoiceDto.PhoneNumber,
                Email = null,
                UserStatus = null,
                Role = UserRole.Customer,
                UserType = UserType.Registered,
                CreatedAt = DateTime.UtcNow
            };

            // customer mocking
            _mockUserService
                .Setup(x =>
                x.GetOrCreateGuestUserAsync(invoiceDto.FirstName, invoiceDto.LastName, invoiceDto.PhoneNumber))
                .ReturnsAsync(userDto);

            //numberGenerator mocking
            _mockNumberGenerator.Setup(x => x.GenerateInvoiceNumberAsync())
                .ReturnsAsync(invoiceNumber);


            //mocking stockService
            _mockStockService.Setup(x =>
            x.ConfirmSaleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .ReturnsAsync((Guid productId, Guid performedBy, int quantity)
                => new SaleDto
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = 100 //whatever price, price is StockService's concern
                });

            //mocking InvoiceRepo
            _mockInvoiceRepo
                .Setup(x => x.AddAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            // made the last data base call before commit transaction throws exception, should rollback
            _mockAuditLogService
                .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                )).Throws<Exception>();

            // mock UnitOfWork(transactions)
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act
            var act= async ()=>await _invoiceService.CreateInvoiceAsync(invoiceDto, Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();

            _mockInvoiceRepo.Verify(x => x.AddAsync(It.IsAny<Invoice>()), Times.Once);
            // Called twice: success attempt (before exception) and failure log (in catch)
            _mockAuditLogService.Verify(x => x.LogAuditAsync(
                It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                ), Times.Exactly(2));

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
        [Fact]
        public async Task CreateReservationInvoiceAsync_WithValidData_CreatesInvoice()
        {
            // Arrange
            var reservationDto = new CreateReservationInvoiceDto
            {
                ReservationNumber = "RES-20250505-0001",
                CustomerId = Guid.NewGuid(),
                OrderSource = RequestSource.InStore,
                PaymentMethod = PaymentMethod.Cash,
                ProductId = Guid.NewGuid(),
                Quantity = 1,
                UnitPrice = 1000
            };
            var performedBy = Guid.NewGuid();

            _mockInvoiceRepo.Setup(x => x.AddAsync(It.IsAny<Invoice>()))
                .Returns(Task.CompletedTask);

            _mockAuditLogService
               .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
               It.IsAny<AuditLogType>(),
               It.IsAny<string>(),
               It.IsAny<bool>()
               )).Returns(Task.CompletedTask);

            // Act
            var invoiceDto = await _invoiceService.CreateReservationInvoiceAsync(reservationDto, performedBy);

            // Assert
            invoiceDto.CustomerId.Should().Be(reservationDto.CustomerId);
            invoiceDto.OrderSource.Should().Be(reservationDto.OrderSource);
            invoiceDto.PaymentMethod.Should().Be(reservationDto.PaymentMethod);
            invoiceDto.CreatedBy.Should().Be(performedBy);

            // Verify
            _mockAuditLogService.Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
               It.IsAny<AuditLogType>(),
               It.IsAny<string>(),
               It.IsAny<bool>()),
               Times.Once());

            _mockInvoiceRepo.Verify(x => x.AddAsync(It.IsAny<Invoice>()),Times.Once());

        }

        [Fact]
        public async Task CancelInvoiceAsync_WhenCanCancel_CancelsInvoice()
        {
            // Arrange
            var invoice = new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId: Guid.NewGuid(),
                orderSource:RequestSource.InStore,
                createdBy:Guid.NewGuid(),
                paymentMethod:PaymentMethod.Cash
                );

            _mockInvoiceRepo.Setup(x => x.GetByNumberAsync(invoice.InvoiceNumber))
                .ReturnsAsync(invoice);

            _mockStockService.Setup(x => x.RefundProductAsync(It.IsAny<Guid>(),
                It.IsAny<int>(), It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            invoice.AddItem(Guid.NewGuid(), 5, 1000);
            invoice.AddItem(Guid.NewGuid(), 10, 2000);

            // Act
           await _invoiceService.CancelInvoiceAsync(invoice.InvoiceNumber,Guid.NewGuid());

            // Assert
            invoice.Status.Should().Be(InvoiceStatus.Cancelled);

        } 
        [Fact]
        public async Task CancelInvoiceAsync_WhenInvoiceNotFound_ThrowsException()
        {
            // Arrange
            var invoice = new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId: Guid.NewGuid(),
                orderSource:RequestSource.InStore,
                createdBy:Guid.NewGuid(),
                paymentMethod:PaymentMethod.Cash
                );

            _mockInvoiceRepo.Setup(x => x.GetByNumberAsync(invoice.InvoiceNumber))
                .ReturnsAsync((Invoice?)null);


            // Act
          var act=async()=> await _invoiceService.CancelInvoiceAsync(invoice.InvoiceNumber, invoice.CreatedBy);

            // Assert

            await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage($"Invoice with number {invoice.InvoiceNumber} is not found!");

            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);

        }

        [Fact]
        public async Task GetInvoicesByCustomerAsync_WithExistingCustomer_ReturnsInvoices()
        {
            //  Arrange
            var customerId= Guid.NewGuid();
            var expectedInvoices = new List<Invoice>()
            {
               new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId:customerId,
                orderSource:RequestSource.InStore,
                createdBy:Guid.NewGuid(),
                paymentMethod:PaymentMethod.Cash
                ),
                new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId:customerId,
                orderSource:RequestSource.InStore,
                createdBy:Guid.NewGuid(),
                paymentMethod:PaymentMethod.Cash
                ),
                new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId:customerId,
                orderSource:RequestSource.InStore,
                createdBy:Guid.NewGuid(),
                paymentMethod:PaymentMethod.Cash
                )
            };

            _mockInvoiceRepo.Setup(x => x.GetByCustomerIdAsync(customerId))
                .ReturnsAsync(expectedInvoices);
            
            // Act
            var actuallyInvoices = await _invoiceService.GetInvoicesByCustomerAsync(customerId);

            // Assert
            var expectedInvoicesDtos = expectedInvoices.Select(invoice => invoice.ToDto());

            actuallyInvoices.Should().BeEquivalentTo(expectedInvoicesDtos);
        }
        
        [Fact]
        public async Task GetInvoicesByCustomerAsync_WithExceptionThrown_CatchesException()
        {
            //  Arrange
            var customerId= Guid.NewGuid();

            _mockInvoiceRepo.Setup(x => x.GetByCustomerIdAsync(customerId))
                .ThrowsAsync(new Exception());
            
            // Act
            var act =async ()=> await _invoiceService.GetInvoicesByCustomerAsync(customerId);

            // Assert
           await  act.Should().ThrowAsync<Exception>()
                .WithMessage($"Failed to retrieve invoices for customer with id: {customerId}");

            _mockInvoiceRepo.Verify(x => x.GetByCustomerIdAsync(customerId), Times.Once);

        }
        [Fact]
        public async Task GetInvoiceById_WithExistingInvoice_ReturnsInvoice()
        {
            //  Arrange
            var expectedInvoice = new Invoice(
                 invoiceNumber: "INV-20250505-0001",
                 customerId: Guid.NewGuid(),
                 orderSource: RequestSource.InStore,
                 createdBy: Guid.NewGuid(),
                 paymentMethod: PaymentMethod.Cash
                 );

            _mockInvoiceRepo.Setup(x => x.GetByIdAsync(expectedInvoice.Id))
                .ReturnsAsync(expectedInvoice);
            
            // Act
            var actuallyInvoice = await _invoiceService.GetInvoiceByIdAsync(expectedInvoice.Id);

            // Assert

            actuallyInvoice.Should().BeEquivalentTo(expectedInvoice.ToDto());
        }
        
        [Fact]
        public async Task GetInvoiceById_WhenInvoiceNotFound_ThrowsException()
        {
            //  Arrange

            var invoiceId = Guid.NewGuid();

            _mockInvoiceRepo.Setup(x => x.GetByIdAsync(invoiceId))
                .ReturnsAsync((Invoice?)null);
            
            // Act
            var act =async ()=> await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            // Assert
           await  act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Invoice with id: {invoiceId} is not found!");

            _mockInvoiceRepo.Verify(x => x.GetByIdAsync(invoiceId), Times.Once);

        }  
        [Fact]
        public async Task GetInvoiceByNumber_WithExistingInvoice_ReturnsInvoice()
        {
            //  Arrange
            var expectedInvoice = new Invoice(
                 invoiceNumber: "INV-20250505-0001",
                 customerId: Guid.NewGuid(),
                 orderSource: RequestSource.InStore,
                 createdBy: Guid.NewGuid(),
                 paymentMethod: PaymentMethod.Cash
                 );

            _mockInvoiceRepo.Setup(x => x.GetByNumberAsync(expectedInvoice.InvoiceNumber))
                .ReturnsAsync(expectedInvoice);
            
            // Act
            var actuallyInvoice = await _invoiceService.GetInvoiceByNumberAsync(expectedInvoice.InvoiceNumber);

            // Assert

            actuallyInvoice.Should().BeEquivalentTo(expectedInvoice.ToDto());
        }
        
        [Fact]
        public async Task GetInvoiceByNumber_WhenInvoiceNotFound_ThrowsException()
        {
            //  Arrange

            var invoiceNumber = "INV-20250505-0001";

            _mockInvoiceRepo.Setup(x => x.GetByNumberAsync("INV-20250505-0001"))
                .ReturnsAsync((Invoice?)null);
            
            // Act
            var act =async ()=> await _invoiceService.GetInvoiceByNumberAsync("INV-20250505-0001");

            // Assert
           await  act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Invoice with number {invoiceNumber} is not found!");

        }
        [Fact]
        public async Task RemoveItemFromInvoiceAsync_WhenInvoiceItemExists_RemovesItem()
        {
            // Arrange
            var itemToRemove = Guid.NewGuid();

            var invoice = new Invoice(
                 invoiceNumber: "INV-20250505-0001",
                 customerId: Guid.NewGuid(),
                 orderSource: RequestSource.InStore,
                 createdBy: Guid.NewGuid(),
                 paymentMethod: PaymentMethod.Cash
                 );
            invoice.AddItem(itemToRemove, 10, 10);
            invoice.AddItem(Guid.NewGuid(), 50, 5);


            _mockInvoiceRepo.Setup(x=>x.GetByNumberAsync(invoice.InvoiceNumber))
                .ReturnsAsync(invoice);

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var expectedInvoice = await _invoiceService
                .RemoveItemFromInvoiceAsync(invoice.InvoiceNumber, itemToRemove,Guid.NewGuid());
            
            // Assert

            invoice.Items.Should().HaveCount(1);
        }
        
        [Fact]
        public async Task RemoveItemFromInvoiceAsync_WhenInvoiceItemNotFound_ThrowsException()
        {
            // Arrange
            var itemToRemove = Guid.NewGuid();

            var invoice = new Invoice(
                 invoiceNumber: "INV-20250505-0001",
                 customerId: Guid.NewGuid(),
                 orderSource: RequestSource.InStore,
                 createdBy: Guid.NewGuid(),
                 paymentMethod: PaymentMethod.Cash
                 );
            invoice.AddItem(Guid.NewGuid(), 10, 10);
            invoice.AddItem(Guid.NewGuid(), 50, 5);


            _mockInvoiceRepo.Setup(x => x.GetByNumberAsync(invoice.InvoiceNumber))
                .ReturnsAsync(invoice);


            // Act
            var act =async ()=> await _invoiceService
                .RemoveItemFromInvoiceAsync(invoice.InvoiceNumber, itemToRemove,Guid.NewGuid()  );

            // Assert
            await act.Should().ThrowAsync<Exception>();//exception type is domain concern not service's 

            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);

            invoice.Items.Should().HaveCount(2);
        }
    }
}
