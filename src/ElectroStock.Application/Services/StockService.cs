using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;

namespace ElectroStock.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IProductRepository _productRepo;
        private readonly IStockMovementRepository _stockMovementRepo;
        private readonly IUnitOfWork _unitOfWork;

        public StockService(IProductRepository productRepo, 
            IStockMovementRepository stockMovementRepo,
            IUnitOfWork unitOfWork
            )
        {
            _productRepo = productRepo;
            _stockMovementRepo = stockMovementRepo;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Finalizes a sale by mutating the product state and persisting the movement record.
        /// The quantity-before and quantity-after values are tracked explicitly so the stock log reflects the delta accurately.
        /// </summary>
        public async Task<SaleDto> ConfirmSaleAsync(Guid productId,Guid performedBy, int quantity=1)
        {
            try
            {
                 var product=await _productRepo.GetByIdAsync(productId);
              
                if (product == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");


                int quantityBefore = 0;

                if (product.ProductType == ProductType.Serialized)
                {
                    quantityBefore = 1;
                }
                else
                {
                    quantityBefore = ((QuantifiedProduct)product).QuantityOnHand;
                }

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                product.ConfirmSale(quantity);
                _productRepo.Update(product);

              

                await _stockMovementRepo.AddAsync(
                    new StockMovement(
                   productId: productId,
                   quantity: quantity,
                   quantityBefore: quantityBefore,
                   quantityAfter:quantityBefore-quantity,
                   referenceType:AuditLogType.StockSale,
                   performedBy:performedBy,
                   details:$"Success sale confirmation for product with id: {productId}, quantity: {quantity}," +
                   $" performed by: {performedBy}"
                   ));

                return new SaleDto
                {
                    ProductId=product.Id,
                    Quantity=quantity,
                    UnitPrice=product.UnitPrice
                };
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to confirm sale for product with id: {productId}, quantity: {quantity}",ex);
            }
        }
        // Confirms the already-reserved sale path and records the stock movement as a reservation confirmation.
        public async Task<SaleDto> ConfirmReservedSaleAsync(Guid productId, Guid performedBy, int quantity = 1)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");

                int quantityBefore = 0;

                if (product.ProductType == ProductType.Serialized)
                {
                    quantityBefore = 1;
                }
                else
                {
                    quantityBefore = ((QuantifiedProduct)product).QuantityOnHand;
                }

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                product.ConfirmReservedSale(quantity);
                _productRepo.Update(product);

                await _stockMovementRepo.AddAsync(
                    new StockMovement(
                   productId: productId,
                   quantity: quantity,
                   quantityBefore: quantityBefore,
                   quantityAfter: quantityBefore - quantity,
                   referenceType: AuditLogType.ReservationConfirmation,
                   performedBy: performedBy,
                   details: $"Success sale confirmation for product with id: {productId}, quantity: {quantity}," +
                   $" performed by: {performedBy}"
                   ));

                return new SaleDto
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.UnitPrice
                };
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to confirm sale for product with id: {productId}, quantity: {quantity}",ex);
            }
        }

        public async Task<ProductDto> ReceiveSerializedProductAsync(string sku, CreateSerializedProductDto productDto, Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var productData = productDto.ToCreationData(sku);

                var product = new SerializedProduct(
                    productData: productData,
                    serialNumber: productDto.SerialNumber,
                    imei: productDto.Imei,
                    warrantyPeriod: productDto.WarrantyPeriod
                    );

                await _productRepo.AddAsync(product);

                await _stockMovementRepo.AddAsync(new StockMovement(
                   productId: product.Id,
                   quantity: 1,
                   quantityBefore: 0,
                   quantityAfter: 1,
                   referenceType: AuditLogType.StockReceipt,
                   performedBy: performedBy,
                   details: $"Success stock receipt for serialized product with serial number: {product.SerialNumber}, imei: {product.Imei}," +
                   $" performed by: {performedBy}"
                   ));

                await _unitOfWork.CommitTransactionAsync();
                return product.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to receive serialized product with serial number: {productDto.SerialNumber}",ex);
            }
        }


        // The transaction flag allows this call to participate in a larger workflow without starting a nested transaction.
        public async Task<StockDto> ReceiveQuantifiedProductAsync(string sku, int quantity, Guid performedBy,bool isInTransaction=false)
        {
            if(!isInTransaction)
                await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = (await _productRepo.GetBySkuAsync(sku));

                if (!result.Any())
                    throw new KeyNotFoundException($"Error!: product with sku: {sku} not found!");
               
                var product = result.First();
                
                if (product.ProductType != ProductType.Quantified)
                    throw new InvalidOperationException($"Product with sku: {sku} is {product.ProductType}, not Quantified");

                var quantifiedProduct = (QuantifiedProduct)product;

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                quantifiedProduct.ReceiveQuantity(quantity);
                _productRepo.Update(product);

                await _stockMovementRepo.AddAsync(new StockMovement(
                  productId: product.Id,
                  quantity: quantity,
                  quantityBefore: quantifiedProduct.QuantityOnHand - quantity,
                  quantityAfter: quantifiedProduct.QuantityOnHand,
                  referenceType: AuditLogType.StockReceipt,
                  performedBy: performedBy,
                  details: $"Success stock receipt for quantified product with id: {product.Id}, quantity: {quantity}" +
                  $" performed by: {performedBy}"
                  ));

                if (!isInTransaction)
                    await _unitOfWork.CommitTransactionAsync();

                return new StockDto
                {
                    Sku=product.Sku,
                    QuantityOnHand = quantifiedProduct.QuantityOnHand,
                    QuantityReserved= quantifiedProduct.QuantityReserved,
                    QuantityAvailable= quantifiedProduct.AvailableQuantity
                };

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                if (!isInTransaction)
                    await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                if (!isInTransaction)
                    await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to receive quantified product with sku: {sku}, quantity: {quantity}", ex);
            }
        }

        public async Task ReserveStockAsync(Guid productId, Guid performedBy, int quantity = 1)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");

                product.Reserve(quantity);
                _productRepo.Update(product );

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to reserve product with id: {productId}, quantity: {quantity}",ex);
            }
        }
        /// <summary>
        /// Restores inventory for a sold or removed quantity and records the refund as a stock movement.
        /// This is used by invoice cancellation and item removal, so the method must preserve the product's quantity accounting consistently.
        /// </summary>
        public async Task RefundProductAsync(Guid productId, int quantity,Guid performedBy)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");

                product.Refund(quantity);
                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                _productRepo.Update(product);

                int quantityBefore = 0;
                int quantityAfter = 0;
                if (product.ProductType == ProductType.Serialized)
                {
                    quantityAfter = 1;
                }
                else
                {
                    quantityBefore = ((QuantifiedProduct)product).QuantityOnHand-quantity;
                    quantityAfter = ((QuantifiedProduct)product).QuantityOnHand;
                }

                await _stockMovementRepo.AddAsync(new StockMovement(
                     productId: product.Id,
                     quantity: quantity,
                     quantityBefore: quantityBefore,
                     quantityAfter: quantityAfter,
                     referenceType: AuditLogType.StockRefund,
                     performedBy: performedBy,
                     details: $"Success stock receipt for product with id: {product.Id}, quantity: {quantity}" +
                     $" performed by: {performedBy}"
                     ));

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to release reservation for product with id: {productId}, quantity: {quantity}", ex);
            }
        }

        public async Task ReleaseReservedProductAsync(Guid productId, int quantity)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                product.ReleaseReservation(quantity);
                _productRepo.Update(product);
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to release reservation for product with id: {productId}, quantity: {quantity}", ex);
            }
        }
        public async Task<StockDto> GetCurrentStockAsync(string sku)
        {
            try
            {
                var products=await _productRepo.GetBySkuAsync(sku);

                if(!products.Any())
                    throw new KeyNotFoundException($"Product with SKU {sku} not found!");

                var first=products.First();

                if (first.ProductType == ProductType.Quantified) {
                    var quantified = (QuantifiedProduct)first;
                    return new StockDto
                    {
                        Sku = sku,
                        QuantityOnHand = quantified.QuantityOnHand,
                        QuantityReserved = quantified.QuantityReserved,
                        QuantityAvailable = quantified.AvailableQuantity,
                    };
                }
                else {
                   
                    var serializedProducts = products.Cast<SerializedProduct>();

                    var onHand = serializedProducts.Count(p=>p.Status!=ProductStatus.SoldOut);

                    var reserved= serializedProducts.Count(p=>
                    p.Status == ProductStatus.Reserved);

                    var available = serializedProducts.Count(p =>
                    p.Status == ProductStatus.Active);

                    return new StockDto
                    {
                        Sku = sku,
                        QuantityOnHand = onHand,
                        QuantityReserved = reserved,
                        QuantityAvailable = available,

                    };
                }
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new Exception($"Failed to retrieve current stock for product with sku: {sku}", ex);
            }
        }

        public async Task<IEnumerable<StockMovementDto>> GetStockMovementByDateRangeAsync(Guid productId, DateTime start, DateTime end)
        {
            try
            {
                if (await _productRepo.GetByIdAsync(productId) == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");

                var stockMovements= await _stockMovementRepo.GetByDateRangeAsync(productId,start, end);

                return stockMovements.Select(s => s.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve stock movement for product with id {productId} " +
                    $"in time range, start: {start}, end: {end} !",ex);
            }
        }

        public async Task<IEnumerable<StockMovementDto>> GetStockMovementHistoryAsync(Guid productId)
        {
            try
            {
                if (await _productRepo.GetByIdAsync(productId) == null)
                    throw new KeyNotFoundException($"Error!: product with Id {productId} not found!");

                var stockMovements = await _stockMovementRepo.GetByProductIdAsync(productId);
                return stockMovements.Select(s => s.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve stock movement history, for product with id: {productId} !", ex);
            }
        }

       
    }
}
