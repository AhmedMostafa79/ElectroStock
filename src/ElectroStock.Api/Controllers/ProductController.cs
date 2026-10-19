using ElectroStock.API.Helpers;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles product-related operations including creation, retrieval, and management.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for managing products in the inventory system.
    /// Different endpoints have different authorization requirements based on the operation.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class ProductController:ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Creates a new quantified product (e.g., accessories, cables, chargers).
        /// </summary>
        /// <remarks>
        /// Creates a new quantified product with stock quantity tracking. Quantified products
        /// are tracked by quantity (e.g., 100 units of USB-C cables) rather than individual serial numbers.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/product/quantified
        /// Authorization: Bearer {admin-token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "sku": "CBL-USBC-001",
        ///   "brand": "Anker",
        ///   "name": "USB-C Fast Charging Cable",
        ///   "category": "Accessory",
        ///   "model": "A1234",
        ///   "unitCost": 8.50,
        ///   "unitPrice": 19.99,
        ///   "initialQuantity": 100,
        ///   "minimumStockLevel": 20
        /// }
        /// </remarks>
        /// <param name="productDto">The product creation details (SKU, brand, name, category, model, costs, initial quantity, minimum stock level)</param>
        /// <returns>The newly created quantified product with all details</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("quantified")]
        public async Task<ActionResult<ProductDto>> CreateQuantifiedProductAsync([FromBody] CreateQuantifiedProductDto productDto)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var product = await _productService.CreateQuantifiedProductAsync(productDto, performedBy);
                return Ok(product);
        }

        /// <summary>
        /// Retrieves a product by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Gets detailed information about a specific product including its type (quantified or serialized)
        /// and current stock status.
        /// Users with Admin, Warehouse, or Cashier roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/product/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="productId">The unique identifier (GUID) of the product to retrieve</param>
        /// <returns>Detailed product information including type, stock, and pricing</returns>
        [Authorize(Roles = "Admin,Warehouse,Cashier")]
        [HttpGet("{productId}")]
        public async Task<ActionResult<ProductDto>> GetProductByIdAsync([FromRoute]Guid productId)
        {
                var product = await _productService.GetProductByIdAsync(productId);
                return Ok(product);
        }

        /// <summary>
        /// Retrieves products by SKU (Stock Keeping Unit).
        /// </summary>
        /// <remarks>
        /// Gets a list of products matching the provided SKU (exact match). Since SKU is typically unique,
        /// this usually returns a single product if quantified product, a list of products if serialized product.
        /// Users with Admin, Warehouse, or Cashier roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/product/sku/CBL-USBC-001
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="sku">The unique SKU code of the product to retrieve</param>
        /// <returns>List of products with the matching SKU</returns>
        [Authorize(Roles = "Admin,Warehouse,Cashier")]
        [HttpGet("sku/{sku}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductBySkuAsync([FromRoute]string sku)
        {
                var products = await _productService.GetProductsBySkuAsync(sku);
                return Ok(products);
        }

        /// <summary>
        /// Retrieves all products in a specific category.
        /// </summary>
        /// <remarks>
        /// Gets a list of all products belonging to the specified product category.
        /// Users with Admin, Warehouse, or Cashier roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/product/category/Smartphone
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="category">The product category (e.g., Smartphone, Accessory, Tablet, Laptop)</param>
        /// <returns>List of products in the specified category</returns>
        [Authorize(Roles = "Admin,Warehouse,Cashier")]
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategoryAsync([FromRoute]ProductCategory category)
        {
                var products = await _productService.GetProductsByCategoryAsync(category);
                return Ok(products);
        }

        /// <summary>
        /// Retrieves all products in the inventory system.
        /// </summary>
        /// <remarks>
        /// Gets a complete list of all products regardless of category or type.
        /// Users with Admin, Warehouse, or Cashier roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/product
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <returns>List of all products in the system</returns>
        [Authorize(Roles = "Admin,Warehouse,Cashier")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProductsAsync()
        {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
        }

        /// <summary>
        /// Updates the selling price for all products with the specified SKU.
        /// </summary>
        /// <remarks>
        /// Changes the unit price (selling price) for all products matching the provided SKU.
        /// This affects future sales only. Historical invoice data remains unchanged.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/product/CBL-USBC-001/price?price=24.99
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="sku">The SKU of the product(s) to update</param>
        /// <param name="price">The new selling price (must be greater than or equal to unit cost)</param>
        /// <returns>Success message confirming the price update</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("{sku}/price")]
        public async Task<ActionResult> ChangeProductPriceAsync([FromRoute] string sku, [FromQuery] decimal price)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            await _productService.UpdateProductsPriceAsync(sku, price, performedBy);
                return Ok(new
                {
                    message = $"Price for products with SKU: {sku} updated successfully"
                });
        }

        /// <summary>
        /// Updates the cost price for all products with the specified SKU.
        /// </summary>
        /// <remarks>
        /// Changes the unit cost (purchase price) for all products matching the provided SKU.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/product/CBL-USBC-001/cost?cost=9.50
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="sku">The SKU of the product(s) to update</param>
        /// <param name="cost">The new cost price (must be greater than zero)</param>
        /// <returns>Success message confirming the cost update</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("{sku}/cost")]
        public async Task<ActionResult> ChangeProductCostAsync([FromRoute] string sku, [FromQuery] decimal cost)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            await _productService.UpdateProductsCostAsync(sku, cost, performedBy);
                return Ok(new
                {
                    message = $"Cost for products with SKU: {sku} updated successfully"
                });
        }

        /// <summary>
        /// Discontinues a product.
        /// </summary>
        /// <remarks>
        /// Marks a product as discontinued, making it unavailable for sale nor reservation.
        /// Discontinued products remain in the system for historical purposes but cannot be added to new invoices.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/product/3fa85f64-5717-4562-b3fc-2c963f66afa6/discontinue
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="productId">The unique identifier of the product to discontinue</param>
        /// <returns>Success message confirming the product was discontinued</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("{productId}/discontinue")]
        public async Task<ActionResult> DiscontinueProductAsync([FromRoute] Guid productId)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            await _productService.DiscontinueProductAsync(productId, performedBy);
                return Ok(new
                {
                    message = $"Product with id: {productId} discontinued successfully"
                });
        }

        /// <summary>
        /// Publishes a product, making it available for sale.
        /// </summary>
        /// <remarks>
        /// Publishes a previously unpublished or discontinued product, making it available for sale.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/product/3fa85f64-5717-4562-b3fc-2c963f66afa6/publish
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="productId">The unique identifier of the product to publish</param>
        /// <returns>Success message confirming the product was published</returns>

        [Authorize(Roles = "Admin")]
        [HttpPost("{productId}/publish")]
        public async Task<ActionResult> PublishProductAsync([FromRoute] Guid productId)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            await _productService.PublishProductAsync(productId, performedBy);
                return Ok(new
                {
                    message = $"Product with id: {productId} published successfully"
                });
        }
        
    }
}
