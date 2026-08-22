using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using ElectroStock.Infrastructure;
using ElectroStock.Tests.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.IntegrationTests
{
    [Collection("TestContainers")]
    public class ReservationServiceTests
    {
        private readonly TestContainersFixture _fixture;
        public ReservationServiceTests(TestContainersFixture fixture)
        {
            _fixture=fixture;
            _fixture.CleanDatabaseAsync().GetAwaiter().GetResult();
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
        public async Task ConfirmReservationAsync_ShouldConfirmSale_DeductStock_AndCreateInvoice()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // product setup
            var product = CreateTestSerializedProduct();
            product.Publish();
            product.Reserve();

           

            await dbContext.Products.AddAsync(product);
            await dbContext.SaveChangesAsync();

            // reservation setup
            var reservation = new Reservation(
                reservationNumber: "RES-20250505-0001",
                productId: product.Id,
                customerId: Guid.NewGuid(),
                quantity: 1,
                unitPrice: product.UnitPrice,
                source: RequestSource.InStore
                );

            await dbContext.Reservations.AddAsync(reservation);
            await dbContext.SaveChangesAsync();

            var paymentMethod = PaymentMethod.Cash;
            var confirmedBy = Guid.NewGuid();
            var beforeStock = await dbContext.Products
                .OfType<SerializedProduct>()
                .AsNoTracking()
                .FirstAsync(p => p.Id == product.Id);

            // Act
            var reservationDto = await reservationService
                .ConfirmReservationAsync(reservation.ReservationNumber, paymentMethod, confirmedBy);

            // Assert
            
            var afterStock = await dbContext.Products
                .OfType<SerializedProduct>()
                .AsNoTracking()
                .FirstAsync(p => p.Id == product.Id);

            beforeStock.Status.Should().Be(ProductStatus.Reserved);
            afterStock.Status.Should().Be(ProductStatus.SoldOut);

            var savedReservation = await dbContext.Reservations
                .FirstOrDefaultAsync(r => r.ReservationNumber == reservation.ReservationNumber);

            savedReservation.Status.Should().Be(ReservationStatus.Confirmed);

            var stockMovement =await dbContext.StockMovements
                .FirstOrDefaultAsync(m=>m.ProductId== product.Id);

            stockMovement.ProductId.Should().Be(product.Id);
            stockMovement.ReferenceType.Should().Be(AuditLogType.ReservationConfirmation);
            stockMovement.Quantity.Should().Be(1);
            stockMovement.QuantityBefore.Should().Be(1);
            stockMovement.QuantityAfter.Should().Be(0);

            var invoiceDto=await dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i=>i.CustomerId == reservation.CustomerId);

            invoiceDto.TotalPrice.Should().Be(product.UnitPrice);
            invoiceDto.OrderSource.Should().Be(reservation.Source);
            invoiceDto.CreatedBy.Should().Be(confirmedBy);
            invoiceDto.PaymentMethod.Should().Be(paymentMethod);
            var auditLog = await dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.ReferenceType == AuditLogType.ReservationConfirmation);

            auditLog.Details.Should().Contain($"{reservation.CustomerId}");
            auditLog.Details.Should().Contain(reservation.ReservationNumber);
            auditLog.Details.Should().Contain(invoiceDto.InvoiceNumber);


        }
        [Fact]
        public async Task CancelReservationAsync_ReleaseReservation_AndStock_AndLogAudit()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // product setup
            var product = CreateTestSerializedProduct();
            product.Publish();
            product.Reserve();

            await dbContext.Products.AddAsync(product);
            await dbContext.SaveChangesAsync();

            // reservation setup
            var reservation = new Reservation(
                reservationNumber: "RES-20250505-0001",
                productId: product.Id,
                customerId: Guid.NewGuid(),
                quantity: 1,
                unitPrice: product.UnitPrice,
                source: RequestSource.InStore
                );

            await dbContext.Reservations.AddAsync(reservation);
            await dbContext.SaveChangesAsync();

            var beforeStock = await dbContext.Products
                .OfType<SerializedProduct>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.Id==product.Id);
            var cancelledBy=Guid.NewGuid();
            
            // Act
            var reservationDto = await reservationService
                .CancelReservationAsync(reservation.ReservationNumber, cancelledBy);

            // Assert

            // verify stock

            var afterStock= await dbContext.Products
                .OfType<SerializedProduct>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            beforeStock.Status.Should().Be(ProductStatus.Reserved);
            afterStock.Status.Should().Be(ProductStatus.Active);

            // verify reservation
            var savedReservation = await dbContext.Reservations
                .FirstOrDefaultAsync(r => r.ReservationNumber == reservationDto.ReservationNumber);

            savedReservation.Status.Should().Be(ReservationStatus.Cancelled);
            savedReservation.ReservationNumber.Should().Be(reservation.ReservationNumber);


            // verify release auditLog
            var auditLog = await dbContext.AuditLogs
               .FirstOrDefaultAsync(a => a.ReferenceType == AuditLogType.ReservationCancellation);

            auditLog.Details.Should().Contain($"{reservation.CustomerId}");
            auditLog.Details.Should().Contain(reservation.ReservationNumber);

        }
    }
}
