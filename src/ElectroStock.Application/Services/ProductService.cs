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
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IStockService _stockService;
        public ProductService(IUnitOfWork unitOfWork, IProductRepository productRepo, IAuditLogService auditLogService, IStockService stockService)
        {
            _unitOfWork = unitOfWork;
            _productRepo = productRepo;
            _auditLogService = auditLogService;
            _stockService = stockService;
        }
        /// <summary>
        /// Creates the catalog item first and applies the initial stock receipt in the same transaction when provided.
        /// </summary>
        public async Task<ProductDto> CreateQuantifiedProductAsync(CreateQuantifiedProductDto productDto,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var productData = productDto.ToCreationData();

                var product = new QuantifiedProduct(
                    productData: productData,
                    initialQuantity: 0,
                    minimumStockLevel: productDto.MinimumStockLevel
                    );

                await _productRepo.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();
                if (productDto.InitialQuantity> 0) //record stockMovement if received stock
                {
                    await _stockService.ReceiveQuantifiedProductAsync(product.Sku, productDto.InitialQuantity, performedBy,isInTransaction:true);
                }

                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType:AuditLogType.ProductCreation,
                    details:$"Successfully created quantified product with id: {product.Id}, performed by: {performedBy}",
                    isInTransaction:true
                    );

                await _unitOfWork.CommitTransactionAsync();

                return product.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
               await  _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductCreation,
                     details: $"Failed to create quantified product with sku: {productDto.Sku}, performed by: {performedBy}",
                     isInTransaction: false
                     );

                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductCreation,
                     details: $"Failed to create quantified product with sku: {productDto.Sku}, performed by: {performedBy}",
                     isInTransaction: false
                     );

                throw new Exception($"Failed to create quantified product with name: {productDto.Name}",ex);
            }
        }
       
        public async Task<ProductDto> GetProductByIdAsync(Guid productId)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                
                if (product == null)
                    throw new KeyNotFoundException($"Product with id {productId} not found!");

                return product.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve product with id: {productId}",ex);
            }
        }
        public async Task<IEnumerable<ProductDto>> GetProductsBySkuAsync(string sku)
        {
            try
            {
                var products = await _productRepo.GetBySkuAsync(sku);

                return products.Select(p=>p.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve product with sku: {sku}",ex);
            }
        }
        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(ProductCategory category)
        {
            try
            {

                var matchingProducts = await _productRepo.GetByCategoryAsync(category);

                return matchingProducts.Select(p => p.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve all products",ex);
            }
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            try
            {
                var allProducts = await _productRepo.GetAllAsync();

                return allProducts.Select(p=>p.ToDto());
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve all products",ex);
            }
        }
        /// <summary>
        /// Applies the SKU-wide price change in one batch and records a single audit entry for the batch.
        /// </summary>
        public async Task UpdateProductsPriceAsync(string sku, decimal newPrice,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var products = await _productRepo.GetBySkuAsync(sku);
                
                if (!products.Any())
                    throw new KeyNotFoundException($"Product with sku: {sku}, not found!");

                foreach (var product in products)
                {
                    product.ChangePrice(newPrice);
                }

                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductUpdate,
                     details: $"Successfully updated price for products with sku: {sku}, new price: {newPrice}, performed by: {performedBy}",
                     isInTransaction: true
                     );
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductUpdate,
                     details: $"Failed to update price for products with sku: {sku}, performed by: {performedBy}",
                     isInTransaction: false
                     );
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductUpdate,
                     details: $"Failed to update price for product with sku: {sku}, performed by: {performedBy}",
                     isInTransaction: false
                     );
                throw new Exception($"Failed to update product price, sku: {sku}, price to update: {newPrice}",ex);
            }
        }
        /// <summary>
        /// Applies the SKU-wide cost change in one batch and records a single audit entry for the batch.
        /// </summary>
        public async Task UpdateProductsCostAsync(string sku, decimal newCost, Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var products = await _productRepo.GetBySkuAsync(sku);
                
                if (!products.Any() == null)
                    throw new KeyNotFoundException($"Product with sku: {sku}, not found!");

                foreach (var product in products)
                {
                    product.ChangeCost(newCost);
                }
                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductUpdate,
                     details: $"Successfully updated cost for product with sku: {sku}, new cost: {newCost}, performed by: {performedBy}",
                     isInTransaction: true
                     );
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.ProductUpdate,
                    details: $"Failed to update cost for product with sku: {sku}, performed by: {performedBy}",
                    isInTransaction: false
                    );
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.ProductUpdate,
                    details: $"Failed to update cost for product with sku: {sku}, performed by: {performedBy}",
                    isInTransaction: false
                    );
                throw new Exception($"Failed to update product cost, sku: {sku}, cost to update: {newCost}",ex);
            }
        }

        public async Task DiscontinueProductAsync(Guid productId, Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null)
                    throw new KeyNotFoundException($"Product with id {productId} not found!");

                product.Discontinue();
                _productRepo.Update(product);

                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductUpdate,
                     details: $"Successfully discontinued product with id: {productId}, performed by: {performedBy}",
                     isInTransaction: true
                     );

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.ProductUpdate,
                    details: $"Failed to discontinue product product with id: {productId}, performed by: {performedBy}",
                    isInTransaction: false
                    );
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.ProductUpdate,
                    details: $"Failed to discontinue product product with id: {productId}, performed by: {performedBy}",
                    isInTransaction: false
                    );
                throw new Exception($"Failed to discontinue product with id: {productId}!",ex);
            }
        }
        
        public async Task PublishProductAsync(Guid productId, Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);

                if (product == null)
                    throw new KeyNotFoundException($"Product with id {productId} not found!");

                product.Publish();
                _productRepo.Update(product);

                await _auditLogService.LogAuditAsync(
                     performedBy: performedBy,
                     referenceType: AuditLogType.ProductUpdate,
                     details: $"Successfully published product with id: {productId}, performed by: {performedBy}",
                     isInTransaction: true
                     );

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.ProductUpdate,
                    details: $"Failed to publish product product with id: {productId}, performed by: {performedBy}",
                    isInTransaction: false
                    );
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.ProductUpdate,
                    details: $"Failed to publish product product with id: {productId}, performed by: {performedBy}",
                    isInTransaction: false
                    );
                throw new Exception($"Failed to publish product with id: {productId}!",ex);
            }
        }
    }
}
