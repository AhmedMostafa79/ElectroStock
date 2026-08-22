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
    public class ReservationService:IReservationService
    {
        private readonly IReservationRepository _reservationRepo;
        private readonly ISequenceNumberGenerator _numberGenerator;
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IInvoiceService _invoiceService;
        private readonly IUserService _userService;
        public ReservationService(IReservationRepository reservationRepo, IUnitOfWork unitOfWork,
            ISequenceNumberGenerator numberGenerator, IProductService productService,
            IStockService stockService, IAuditLogService auditLogService, IInvoiceService invoiceService, IUserService userService)
        {
            _reservationRepo = reservationRepo;
            _numberGenerator = numberGenerator;
            _stockService = stockService;
            _productService = productService;
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
            _invoiceService = invoiceService;
            _userService = userService;
        }
        // Temporary P0 concurrency guard: Serializable is used until sequence generation becomes atomic at DB level.
        public async Task<ReservationDto> CreateReservationAsync( CreateReservationDto reservationDto,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync(isolationLevel: IsolationLevel.Serializable);
            try
            {
                var product=await _productService.GetProductByIdAsync(reservationDto.ProductId);

                var customer = await _userService.GetOrCreateGuestUserAsync(reservationDto.FirstName, reservationDto.LastName, reservationDto.PhoneNumber);

                var reservation = new Reservation(
                    reservationNumber: await _numberGenerator.GenerateReservationNumberAsync(),
                    productId: reservationDto.ProductId,
                    customerId: customer.Id,
                    quantity: reservationDto.Quantity,
                    unitPrice: product.UnitPrice,
                    source: reservationDto.Source
                    );


                await _stockService.ReserveStockAsync(reservationDto.ProductId, performedBy, reservationDto.Quantity);

                await _reservationRepo.AddAsync( reservation );

                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                referenceType: AuditLogType.ReservationCreation,
                    details: $"Successfully created reservation for customer with id: {customer.Id}, " +
                    $"product id: {reservationDto.ProductId}, " +
                    $"quantity {reservationDto.Quantity}, " +
                    $"source: {reservationDto.Source}, " +
                    $" performed by: {performedBy}",
                    isInTransaction: true);

                await _unitOfWork.CommitTransactionAsync();

                return reservation.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();

                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                referenceType: AuditLogType.ReservationCreation,
                    details: $"Failed to create reservation for customer with phone: {reservationDto.PhoneNumber}, " +
                    $"product id: {reservationDto.ProductId}, " +
                    $"quantity {reservationDto.Quantity}, " +
                    $"source: {reservationDto.Source}, " +
                    $" performed by: {performedBy}",
                    isInTransaction: false);
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                referenceType: AuditLogType.ReservationCreation,
                    details: $"Failed to create reservation for customer with phone:  {reservationDto.PhoneNumber}, " +
                    $"product id: {reservationDto.ProductId}, " +
                    $"quantity {reservationDto.Quantity}, " +
                    $"source: {reservationDto.Source}, " +
                    $" performed by: {performedBy}",
                    isInTransaction: false);
                
                throw new Exception($"Failed to create reservation for product with id: {reservationDto.ProductId}," +
                    $"order source: {reservationDto.Source}",ex);
            }
        }
        public async Task<ReservationDto> GetReservationByNumberAsync(string reservationNumber )
        {
            try
            {
               var  reservation=await _reservationRepo.GetByNumberAsync(reservationNumber);

                if (reservation == null)
                    throw new KeyNotFoundException($"Reservation with number {reservationNumber} not found!");
                        
                return reservation.ToDto() ;
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve reservation with number: {reservationNumber}",ex);
            }
        }
        public async Task<IEnumerable<ReservationDto>> GetByCustomerAsync(Guid customerId)
        {
            try
            {
                var reservations = await _reservationRepo.GetByCustomerAsync(customerId);

                return reservations.Select(r => r.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve reservations for customer with id: {customerId}", ex);
            }
        }
        public async Task<IEnumerable<ReservationDto>> GetByProductIdAsync(Guid productId)
        {
            try
            {
                var reservations = await _reservationRepo.GetByProductAsync(productId);

                return reservations.Select(r => r.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve reservations for product with id: {productId}", ex);
            }
        }

        // SKU lookup expands to multiple product ids before querying reservations.
        public async Task<IEnumerable<ReservationDto>> GetByProductSkuAsync(string sku)
        {
            try
            {
                var products=await _productService.GetProductsBySkuAsync(sku);
                if(!products.Any())
                    return Enumerable.Empty<ReservationDto>();
                
                var productsIds = products.Select(p => p.Id).ToList();

                var reservations = await _reservationRepo.GetByProductIdsAsync(productsIds);

                return reservations.Select(r => r.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve reservations for product with sku: {sku}", ex);
            }
        }

       
        // Confirms reservation state, finalizes reserved stock, and creates the invoice atomically.
        public async Task<ReservationDto> ConfirmReservationAsync(string reservationNumber,PaymentMethod paymentMethod,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
               var reservation=await _reservationRepo.GetByNumberAsync(reservationNumber);
                
                if(reservation==null)
                    throw new KeyNotFoundException($"Reservation with number {reservationNumber} not found!");

                await _stockService.ConfirmReservedSaleAsync(reservation.ProductId, performedBy, reservation.Quantity);

                reservation.Confirm();
                _reservationRepo.Update(reservation);

               var invoiceDto= await _invoiceService.CreateReservationInvoiceAsync(
                    reservationDto:new CreateReservationInvoiceDto
                    {
                        ReservationNumber=reservation.ReservationNumber,
                        CustomerId=reservation.CustomerId,
                        OrderSource=reservation.Source,
                        PaymentMethod=paymentMethod,
                        ProductId=reservation.ProductId,
                        Quantity=reservation.Quantity,
                        UnitPrice=reservation.UnitPrice
                    },
                    performedBy:performedBy
                    );

                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
                   referenceType: AuditLogType.ReservationConfirmation,
                       details: $"Successfully confirmed reservation with number: {reservationNumber}, " +
                       $"customer id: {reservation.CustomerId}, " +
                       $"invoice number: {invoiceDto.InvoiceNumber}, "+
                       $"performed by: {performedBy}",
                       isInTransaction: true);

                await _unitOfWork.CommitTransactionAsync();

                return reservation.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
               referenceType: AuditLogType.ReservationConfirmation,
                   details: $"Failed to confirm reservation with number: {reservationNumber}," +
                   $" performed by: {performedBy}",
                   isInTransaction: false);
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
               referenceType: AuditLogType.ReservationConfirmation,
                   details: $"Failed to confirm reservation with number: {reservationNumber}," +
                   $" performed by: {performedBy}",
                   isInTransaction: false);

                throw new Exception($"Failed to confirm reservation with number: {reservationNumber}", ex);
            }
        }
        // Cancels reservation state and releases reserved stock in one transaction.
        public async Task<ReservationDto> CancelReservationAsync(string reservationNumber,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reservation = await _reservationRepo.GetByNumberAsync(reservationNumber);
            
                if (reservation == null)
                    throw new KeyNotFoundException($"Reservation with number {reservationNumber} not found!");

                reservation.Cancel(); // no need for Update(reservation) as still being tracked by ChangeTracker
                _reservationRepo.Update(reservation);

                await _stockService.ReleaseReservedProductAsync(reservation.ProductId,reservation.Quantity);

                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                referenceType: AuditLogType.ReservationCancellation,
                    details: $"Successfully cancelled reservation with number: {reservationNumber}," +
                    $" customer with id: {reservation.CustomerId}, " +
                    $"performed by: {performedBy}",
                    isInTransaction: true);

                await _unitOfWork.CommitTransactionAsync();

                return reservation.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
               referenceType: AuditLogType.ReservationCancellation,
                   details: $"Failed to cancel reservation with number: {reservationNumber}," +
                   $" performed by: {performedBy}",
                   isInTransaction: false);
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                  performedBy: performedBy,
              referenceType: AuditLogType.ReservationCancellation,
                  details: $"Failed to cancel reservation with number: {reservationNumber}," +
                  $" performed by: {performedBy}",
                  isInTransaction: false);
                throw new Exception($"Failed to cancel reservation with number: {reservationNumber}", ex);
            }
        }
        // Expires reservation state and releases reserved stock in one transaction.
        public async Task<ReservationDto> ExpireReservationAsync(string reservationNumber,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reservation = await _reservationRepo.GetByNumberAsync(reservationNumber);
                
                if (reservation == null)
                    throw new KeyNotFoundException($"Reservation with number {reservationNumber} not found!");

                reservation.Expire();
                _reservationRepo.Update(reservation);

                await _stockService.ReleaseReservedProductAsync( reservation.ProductId, reservation.Quantity);

                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
               referenceType: AuditLogType.ReservationExpiration,
                   details: $"Successfully expired reservation with number: {reservationNumber} for customer with id: {reservation.CustomerId}, " +
                   $"performed by: {performedBy}",
                   isInTransaction: true);

                await _unitOfWork.CommitTransactionAsync();
                return reservation.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
               referenceType: AuditLogType.ReservationExpiration,
                   details: $"Failed to expire reservation with number: {reservationNumber}," +
                   $" performed by: {performedBy}",
                   isInTransaction: false);
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
               referenceType: AuditLogType.ReservationExpiration,
                   details: $"Failed to expire reservation with number: {reservationNumber}," +
                   $" performed by: {performedBy}",
                   isInTransaction: false);

                throw new Exception($"Failed to expire reservation with number: {reservationNumber}", ex);
            }
        } 
    }
}
