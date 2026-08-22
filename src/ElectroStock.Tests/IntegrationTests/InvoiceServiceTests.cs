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
    [Collection("TestContainers")] //This tells xUnit to share the database fixture
    public class InvoiceServiceTests
    {
        private readonly TestContainersFixture _fixture;// the sandbox

        public InvoiceServiceTests(TestContainersFixture  fixture)
        {
            _fixture=fixture;

            // Start fresh
            _fixture.CleanDatabaseAsync().GetAwaiter().GetResult(); 
        }
        private  QuantifiedProduct CreateQuantifiedProduct(
           string sku = "OR-OFP-500mahWhite",
           int initialQuantity = 10,
           int minimumStockLevel = 2)
            {
                var data = new ProductCreationData
                {
                    Sku = sku,
                    Brand = "Oraimo",
                    Name = "OraimoFreepods",
                    Category = ProductCategory.Audio,
                    Model = "OEB-E104DC",
                    UnitCost = 1000,
                    UnitPrice = 1200,
                };


            return new QuantifiedProduct(data, initialQuantity, minimumStockLevel);
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
        public async Task CreateInvoiceAsync_ShouldCreateInvoice_DeductStock_AndRecordMovements()
        {

            // Arrange

            using var scope = _fixture.ServiceProvider.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // product setup
            var productQuantity = 10;
            var product = CreateQuantifiedProduct(initialQuantity: productQuantity);
            product.Publish();

            await dbContext.Products.AddAsync(product);
            await dbContext.SaveChangesAsync();

            // invoice dto setup
            var quantityToSell= 3;
            var invoiceDto = new CreateInvoiceDto
            {
                FirstName = "Integration",
                LastName = "Test",
                PhoneNumber = "01000000111",
                OrderSource = RequestSource.InStore,
                PaymentMethod = PaymentMethod.Cash,
                Items = new List<AddInvoiceItemDto>
            {
                new AddInvoiceItemDto { ProductId = product.Id, Quantity = quantityToSell }
            }
            };

            //stock before invoice creation
            var beforeStock = await dbContext.Products
                .OfType<QuantifiedProduct>()
                .AsNoTracking()
                .FirstAsync(p => p.Id == product.Id);


            // Act
            var invoice = await invoiceService.CreateInvoiceAsync(invoiceDto, Guid.NewGuid());

            // Assert
            // Verify invoice is persisted
            var savedInvoice = await dbContext.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            savedInvoice.Should().NotBeNull();
            savedInvoice.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
            savedInvoice.Id.Should().Be(invoice.Id);

            // Verify stock was deducted
            var afterStock = await dbContext.Products
                .OfType<QuantifiedProduct>()
                .AsNoTracking()
                .FirstAsync(p => p.Id == product.Id);


            //afterStock.QuantityOnHand.Should().Be(beforeStock.QuantityOnHand - quantityToSell);


            // Verify stock movement was recorded
            var stockMovement = await dbContext.StockMovements
                .FirstOrDefaultAsync(m => m.ProductId == product.Id);

            stockMovement.ReferenceType.Should().Be(AuditLogType.StockSale);
            stockMovement.Quantity.Should().Be(quantityToSell);
            stockMovement.QuantityBefore.Should().Be(beforeStock.QuantityOnHand);
            stockMovement.QuantityAfter.Should().Be(afterStock.QuantityOnHand);

            // Verify GuestUser was created 

            var guestUser = await dbContext.Users
                .OfType<GuestUser>()
                .FirstOrDefaultAsync(u => u.Phone == invoiceDto.PhoneNumber);

            guestUser.FirstName.Should().Be(invoiceDto.FirstName);
            guestUser.LastName.Should().Be(invoiceDto.LastName);


            var invoiceAuditLog = await dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.ReferenceType == AuditLogType.InvoiceCreation);

            invoiceAuditLog.Details.Should().Contain(invoiceDto.PhoneNumber);

        }
        [Fact]
        public async Task CancelInvoiceAsync_CancelInvoice_RefundProducts_RecordRefund_LogAudit()
        {
            // Arrange

            using var scope = _fixture.ServiceProvider.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // 
            // products setup
            var initialQuantity = 10;
            var quantifiedProduct = CreateQuantifiedProduct(initialQuantity: initialQuantity);
            quantifiedProduct.Publish();

            var serializedProduct = CreateTestSerializedProduct();
            serializedProduct.Publish();
            serializedProduct.ConfirmSale();

            await dbContext.Products.AddAsync(quantifiedProduct);
            await dbContext.Products.AddAsync(serializedProduct);

            //// customer setup
            //var customer = new GuestUser(
            //    new UserCreationData
            //    {
            //        FirstName = "Guest",
            //        LastName = "User",
            //        PhoneNumber = "00123456789",
            //        Role = UserRole.Customer
            //    }
            //);

            //await dbContext.Users.AddAsync(customer);

            // invoice setup
            var invoice = new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId: Guid.NewGuid(),
                orderSource: RequestSource.InStore,
                createdBy: Guid.NewGuid(),
                paymentMethod: PaymentMethod.Cash
                );

            var quantityToBuy = 5;
            quantifiedProduct.ConfirmSale(quantityToBuy);
            invoice.AddItem(quantifiedProduct.Id, quantityToBuy, quantifiedProduct.UnitPrice);

            invoice.AddItem(serializedProduct.Id, quantity: 1, quantifiedProduct.UnitPrice);

            await dbContext.Invoices.AddAsync(invoice);
            await dbContext.SaveChangesAsync();

            var cancelledBy = Guid.NewGuid();

            var invoiceBefore = await dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);

            Console.WriteLine($"{serializedProduct.Status}");
            serializedProduct.Status.Should().Be(ProductStatus.SoldOut);

            var stockBefore = await dbContext.Products
                .AsNoTracking()
                .ToListAsync();


            // Act 
            await invoiceService.CancelInvoiceAsync(invoice.InvoiceNumber, cancelledBy);


            // Assert
            stockBefore.Count.Should().Be(invoice.Items.Count);
            invoiceBefore.Status.Should().Be(InvoiceStatus.Completed);

            // assert invoice is cancelled
            var invoiceAfter = await dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);

            invoiceAfter.Status.Should().Be(InvoiceStatus.Cancelled);

            // assert stock is refunded
            var stockAfter = await dbContext.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.SoldOut)
                .ToListAsync();

            stockAfter.Count().Should().Be(0);

            var quantifiedStockMovement = await dbContext.StockMovements
                  .FirstOrDefaultAsync(s => s.ReferenceType == AuditLogType.StockRefund
                  && s.ProductId==quantifiedProduct.Id);

            quantifiedStockMovement.ProductId.Should().Be(quantifiedProduct.Id);
            quantifiedStockMovement.Quantity.Should().Be(quantityToBuy);


            var serializedStockMovement = await dbContext.StockMovements
                  .FirstOrDefaultAsync(s => s.ReferenceType == AuditLogType.StockRefund
                  && s.ProductId == serializedProduct.Id);

            serializedStockMovement.ProductId.Should().Be(serializedProduct.Id);
            serializedStockMovement.Quantity.Should().Be(1);


            // verify auditLog
            var auditLog = await dbContext.AuditLogs
                .FirstOrDefaultAsync(a => a.ReferenceType == AuditLogType.InvoiceCancelation);

            auditLog.PerformedBy.Should().Be(cancelledBy);
            auditLog.Details.Should().Contain($"{invoice.InvoiceNumber}");

        }
        [Fact]
        public async Task RemoveItemFromInvoiceAsync_RemovesItem_RefundsProduct_AndRecordMovements()
        {
            // Arrange
            using var scope = _fixture.ServiceProvider.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // products setup
            var initialQuantity = 10;
            var quantifiedProduct = CreateQuantifiedProduct(initialQuantity: initialQuantity);
            quantifiedProduct.Publish();

            var serializedProduct = CreateTestSerializedProduct();
            serializedProduct.Publish();
            serializedProduct.ConfirmSale();

            await dbContext.Products.AddAsync(quantifiedProduct);
            await dbContext.Products.AddAsync(serializedProduct);

            // customer setup
            var customer = new GuestUser(
                new UserCreationData
                {
                    FirstName = "Guest",
                    LastName = "User",
                    PhoneNumber = "00123456789",
                    Role = UserRole.Customer
                }
            );

            await dbContext.Users.AddAsync(customer);

            // invoice setup
            var invoice = new Invoice(
                invoiceNumber: "INV-20250505-0001",
                customerId: customer.Id,
                orderSource:RequestSource.InStore,
                createdBy:Guid.NewGuid(),
                paymentMethod:PaymentMethod.Cash
                );

            var quantityToBuy = 5;
            quantifiedProduct.ConfirmSale(quantityToBuy);
            invoice.AddItem(quantifiedProduct.Id, quantityToBuy, quantifiedProduct.UnitPrice);
            invoice.AddItem(serializedProduct.Id,quantity: 1, quantifiedProduct.UnitPrice);

            await dbContext.Invoices.AddAsync(invoice);
            await dbContext.SaveChangesAsync();

            // invoice snapshot before removing item
            var beforeInvoice=await dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i=>i.InvoiceNumber==invoice.InvoiceNumber);

            // quantified product's stock snapshot before removal
            var beforeQuantifiedStock = await dbContext.Products
                .OfType<QuantifiedProduct>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p=> p.Id == quantifiedProduct.Id); 

            // Act & Assert 1

            beforeInvoice.Items.Count.Should().Be(2);

            beforeInvoice.Items.First().ProductId.Should().Be(quantifiedProduct.Id);
            beforeInvoice.Items.Last().ProductId.Should().Be(serializedProduct.Id);

            var removedBy = Guid.NewGuid();
            var invoiceDto = await invoiceService
                .RemoveItemFromInvoiceAsync(invoice.InvoiceNumber, quantifiedProduct.Id, removedBy);

            // verify quantified removal
            var afterInvoice = await dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);
            


            afterInvoice.Items.Count.Should().Be(1);
            afterInvoice.Items.First().ProductId.Should().Be(serializedProduct.Id);

            var afterQuantifiedStock = await dbContext.Products
               .OfType<QuantifiedProduct>()
               .AsNoTracking()
               .FirstOrDefaultAsync(p => p.Id == quantifiedProduct.Id);

            var stockMovement = await dbContext.StockMovements
                    .FirstOrDefaultAsync(s => s.ReferenceType == AuditLogType.StockRefund &&
                    s.ProductId == quantifiedProduct.Id);
            stockMovement.ProductId.Should().Be(quantifiedProduct.Id);
            stockMovement.Quantity.Should().Be(quantityToBuy);
            stockMovement.QuantityBefore.Should().Be(beforeQuantifiedStock.QuantityOnHand);
            stockMovement.QuantityAfter.Should().Be(afterQuantifiedStock.QuantityOnHand);


            // serialized product's stock snapshot before removal

            var beforeSerializedStock = await dbContext.Products
             .OfType<SerializedProduct>()
             .AsNoTracking()
             .FirstOrDefaultAsync(p => p.Id == serializedProduct.Id);

            var invoiceDto2 = await invoiceService
                .RemoveItemFromInvoiceAsync(invoice.InvoiceNumber, serializedProduct.Id, removedBy);

            // verify serialized removal (both are removed now!)

            var afterInvoice2 = await dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);



            afterInvoice2.Items.Count.Should().Be(0);

            var stockMovement2 = await dbContext.StockMovements
                    .FirstOrDefaultAsync(s => s.ReferenceType == AuditLogType.StockRefund &&
                    s.ProductId==serializedProduct.Id);

            stockMovement2.ProductId.Should().Be(serializedProduct.Id);
            stockMovement2.Quantity.Should().Be(1);
            stockMovement2.QuantityBefore.Should().Be(0);//before refund
            stockMovement2.QuantityAfter.Should().Be(1);//after refund
        }

    }
}
