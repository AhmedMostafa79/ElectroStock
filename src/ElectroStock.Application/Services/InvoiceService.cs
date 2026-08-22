using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using System.Data;

namespace ElectroStock.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IStockService _stockService;
        private readonly IUserService _userService;
        
        private readonly ISequenceNumberGenerator _numberGenerator;
        private readonly IUnitOfWork _unitOfWork;
        public InvoiceService(IAuditLogService auditLogService, IInvoiceRepository invoiceRepo, IStockService stockService,
            ISequenceNumberGenerator numberGenerator, IUnitOfWork unitOfWork, IUserService userService)
        {
            _auditLogService = auditLogService;
            _invoiceRepo = invoiceRepo;
            _stockService = stockService;
            _numberGenerator = numberGenerator;
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        // Temporary P0 concurrency guard: Serializable is used until sequence generation becomes atomic at DB level.
        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto invoiceDto, Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync(isolationLevel:IsolationLevel.Serializable);
            try
            {
                var customer = await _userService.GetOrCreateGuestUserAsync(invoiceDto.FirstName,invoiceDto.LastName,invoiceDto.PhoneNumber);

               
                 var invoice = new Invoice(
                    invoiceNumber: await _numberGenerator.GenerateInvoiceNumberAsync(),
                    customerId: customer.Id,
                    orderSource: invoiceDto.OrderSource,
                    createdBy: performedBy,
                    paymentMethod: invoiceDto.PaymentMethod
                    );

                     // Each item is processed individually; batch optimization can be considered if order sizes grow.
                foreach(var item in invoiceDto.Items)
                {
                   var saleDto= await _stockService.ConfirmSaleAsync(item.ProductId, performedBy, item.Quantity);
                    invoice.AddItem(saleDto.ProductId,saleDto.Quantity,saleDto.UnitPrice);
                }

                await _invoiceRepo.AddAsync( invoice );

                await _auditLogService.LogAuditAsync(
                            performedBy: performedBy,
                            referenceType: AuditLogType.InvoiceCreation,
                            details: 
                            $"Successfully created invoice with number {invoice.InvoiceNumber}," +
                            $" for customer with phone: {invoiceDto.PhoneNumber}," +
                            $" performed by: {performedBy}" ,
                        isInTransaction:true);

                
                await _unitOfWork.CommitTransactionAsync();

                return invoice.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                              performedBy: performedBy,
                              referenceType: AuditLogType.InvoiceCreation,
                              details:
                             $"Failed to create invoice for customer with phone: {invoiceDto.PhoneNumber}, performed by: {performedBy}",
                          isInTransaction:false);
                
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                               performedBy: performedBy,
                               referenceType: AuditLogType.InvoiceCreation,
                               details:
                              $"Failed to create invoice for customer with phone: {invoiceDto.PhoneNumber}, performed by: {performedBy}",
                           isInTransaction: false);
                throw new Exception($"Failed to create invoice for customer with phone: {invoiceDto.PhoneNumber}",ex);
            }
            
        }
        // Called from reservation confirmation flow where customer/stock checks are already done upstream.
        public async Task<InvoiceDto> CreateReservationInvoiceAsync(CreateReservationInvoiceDto reservationDto, Guid performedBy)
        {
            try
            {
                var invoice = new Invoice(
                   invoiceNumber: await _numberGenerator.GenerateInvoiceNumberAsync(),
                   customerId: reservationDto.CustomerId,
                   orderSource: reservationDto.OrderSource,
                   createdBy: performedBy,
                   paymentMethod: reservationDto.PaymentMethod
                   );
                invoice.AddItem(reservationDto.ProductId, reservationDto.Quantity, reservationDto.UnitPrice);

                await _invoiceRepo.AddAsync(invoice);

                await _auditLogService.LogAuditAsync(
                            performedBy: performedBy,
                            referenceType: AuditLogType.InvoiceCreation,
                            details:
                            $"Successfully created invoice with number {invoice.InvoiceNumber}," +
                            $"reservation with number: {reservationDto.ReservationNumber}, "+
                            $" for customer with id: {reservationDto.CustomerId}," +
                            $" performed by: {performedBy}",
                        isInTransaction: true);

                return invoice.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _auditLogService.LogAuditAsync(
                              performedBy: performedBy,
                              referenceType: AuditLogType.InvoiceCreation,
                              details:
                             $"Failed to create invoice for customer with id: {reservationDto.CustomerId}, " +
                             $"reservation with number: {reservationDto.ReservationNumber}, " +
                             $"performed by: {performedBy}",
                          isInTransaction: false);

                throw;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAuditAsync(
                               performedBy: performedBy,
                               referenceType: AuditLogType.InvoiceCreation,
                               details:
                              $"Failed to create invoice for customer with id:  {reservationDto.CustomerId}," +
                              $"reservation with number: {reservationDto.ReservationNumber}, " +
                              $" performed by: {performedBy}",
                           isInTransaction: false);

                throw new Exception($"Failed to create invoice for customer with id:  {reservationDto.CustomerId}", ex);
            }

        }

        // Cancels the invoice and refunds each line item within the same transaction.
        public async Task CancelInvoiceAsync(string invoiceNumber,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var invoice= await _invoiceRepo.GetByNumberAsync(invoiceNumber);

                if (invoice == null)
                    throw new KeyNotFoundException($"Invoice with number {invoiceNumber} is not found!");

                foreach (var item in invoice.Items)
                {
                     await _stockService.RefundProductAsync(item.ProductId, item.Quantity, performedBy);
                }

                invoice.Cancel();
                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 

                _invoiceRepo.Update(invoice);

                await _auditLogService.LogAuditAsync(
                            performedBy: performedBy,
                            referenceType: AuditLogType.InvoiceCancelation,
                            details:
                            $"Successfully cancelled invoice with number {invoiceNumber}," +
                            $" for customer with id: {invoice.CustomerId}," +
                            $" performed by: {performedBy}",
                        isInTransaction: true);

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                               performedBy: performedBy,
                               referenceType: AuditLogType.InvoiceCancelation,
                               details:
                              $"Failed to cancel invoice with number: {invoiceNumber}, " +
                              $"performed by: {performedBy}",
                           isInTransaction: false);

                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                               performedBy: performedBy,
                               referenceType: AuditLogType.InvoiceCancelation,
                               details:
                              $"Failed to cancel invoice with number: {invoiceNumber}, " +
                              $"performed by: {performedBy}",
                           isInTransaction: false);

                throw new Exception($"Failed to cancel invoice with number: {invoiceNumber}", ex);
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByCustomerAsync(Guid customerId)
        {
            try
            {
                var invoices = await _invoiceRepo.GetByCustomerIdAsync(customerId);

                return invoices.Select(i => i.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve invoices for customer with id: {customerId}",ex);
            }
        }

        public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);

                    if (invoice == null)
                        throw new KeyNotFoundException($"Invoice with id: {invoiceId} is not found!");

                return invoice.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retreive invoice with id: {invoiceId}", ex);
            }
        }

        public async Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            try
            {
                var invoice = await _invoiceRepo.GetByNumberAsync(invoiceNumber);

                if (invoice == null)
                    throw new KeyNotFoundException($"Invoice with number {invoiceNumber} is not found!");

                return invoice.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retreive invoice with number: {invoiceNumber}", ex);
            }
        }

        // Removes one line item and refunds its quantity in the same transaction.
        public async Task<InvoiceDto> RemoveItemFromInvoiceAsync(string invoiceNumber, Guid productId,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var invoice = await _invoiceRepo.GetByNumberAsync(invoiceNumber);

                if (invoice == null)
                    throw new KeyNotFoundException($"Invoice with number {invoiceNumber} is not found!");

               var removedItem=invoice.RemoveItem(productId);
                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                _invoiceRepo.Update(invoice);
                
                await _stockService.RefundProductAsync(productId, removedItem.Quantity, performedBy);

                await _unitOfWork.CommitTransactionAsync();

                return invoice.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to remove item with id {productId} from invoice with number: {invoiceNumber}",ex);
            }
        }
    }
}
