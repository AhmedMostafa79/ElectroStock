using ElectroStock.API.Helpers;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles stock-related operations including receiving, tracking, and inventory management.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for warehouse staff and administrators to manage inventory.
    /// All endpoints require authentication with Warehouse or Admin role.
    /// </remarks>
    [ApiController]
    [Authorize(Roles= "Warehouse,Admin")]
    [Route("api/[controller]")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class StockController:ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }
        /// <summary>
        /// Receives a serialized product (creates product + adds serial numbers).
        /// </summary>
        /// <remarks>
        /// For serialized products (e.g., smartphones, tablets), each unit is unique and tracked by serial number/IMEI.
        /// This endpoint creates and receives the serialized product .
        /// Only users with Warehouse or Admin roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/stock/IPHONE-15-PRO/receive/serialized
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "brand": "Apple",
        ///   "name": "iPhone 15 Pro",
        ///   "category": "Smartphone",
        ///   "model": "A2848",
        ///   "unitCost": 850.00,
        ///   "unitPrice": 999.99,
        ///   "serialNumber": "SN-1234567890",
        ///   "imei": "354123456789012",
        ///   "warrantyPeriod": "OneYear"
        /// }
        /// </remarks>
        /// <param name="sku">The SKU of the product to create/receive</param>
        /// <param name="productDto">The product details including serial numbers</param>
        /// <returns>The newly created serialized product with all serial numbers</returns>
        [HttpPost("{sku}/receive/serialized")]
        public async Task<ActionResult<ProductDto>> ReceiveSerializedProductAsync([FromRoute] string sku,[FromBody] CreateSerializedProductDto productDto)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var product = await _stockService.ReceiveSerializedProductAsync(sku, productDto, performedBy); 
                return Ok(product);
        }

        /// <summary>
        /// Receives stock for an existing quantified product.
        /// </summary>
        /// <remarks>
        /// For quantified products (e.g., accessories, cables), adds bulk quantity to existing stock.
        /// The product must already exist in the system (created via Admin).
        /// Only users with Warehouse or Admin roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/stock/CBL-USBC-001/receive/quantified?quantity=50
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="sku">The SKU of the product to receive stock for</param>
        /// <param name="quantity">The quantity to add to inventory (must be greater than zero)</param>
        /// <returns>Updated stock information including new quantity</returns>
        [HttpPost("{sku}/receive/quantified")]
        public async Task<ActionResult<StockDto>> ReceiveQuantifiedProductAsync([FromRoute] string sku, [FromQuery] int quantity)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var stockDto = await _stockService.ReceiveQuantifiedProductAsync(sku, quantity, performedBy);
                return Ok(stockDto);
        }

        /// <summary>
        /// Retrieves current stock information for a product by SKU.
        /// </summary>
        /// <remarks>
        /// Gets the current stock status including quantity on hand, reserved, and available.
        /// Users with Warehouse or Admin roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/stock/CBL-USBC-001/stock
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="sku">The SKU of the product to check</param>
        /// <returns>
        /// Current stock information including:
        /// - Sku: Product identifier
        /// - QuantityOnHand: Total physical stock
        /// - QuantityReserved: Stock reserved for pending orders
        /// - QuantityAvailable: Stock available for sale (OnHand - Reserved)
        /// </returns>
        [HttpGet("{sku}/stock")]
        public async Task<ActionResult<StockDto>> GetCurrentStockAsync([FromRoute] string sku)
        {
                var stock = await _stockService.GetCurrentStockAsync(sku); 
                return Ok(stock);
        }

        /// <summary>
        /// Retrieves the complete stock movement history for a product.
        /// </summary>
        /// <remarks>
        /// Gets all stock movements (receipts, sales, refunds) for a specific product.
        /// Useful for audit trails and inventory reconciliation.
        /// Users with Warehouse or Admin roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/stock/3fa85f64-5717-4562-b3fc-2c963f66afa6/movements
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="productId">The unique identifier of the product</param>
        /// <returns>List of all stock movements for the product</returns>
        [HttpGet("{productId}/movements")]
        public async Task<ActionResult<IEnumerable<StockDto>>> GetStockMovementsHistoryAsync([FromRoute] Guid productId)
        {
                var stockMovements = await _stockService.GetStockMovementHistoryAsync(productId); 
                return Ok(stockMovements);
        }

        /// <summary>
        /// Retrieves stock movements for a product within a date range.
        /// </summary>
        /// <remarks>
        /// Gets stock movements for a specific product filtered by date range.
        /// Useful for period-specific reports and analysis.
        /// Users with Warehouse or Admin roles are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/stock/3fa85f64-5717-4562-b3fc-2c963f66afa6/movements/by-date?start=2025-01-01&end=2025-01-31
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="productId">The unique identifier of the product</param>
        /// <param name="start">The start date for the range (inclusive)</param>
        /// <param name="end">The end date for the range (inclusive)</param>
        /// <returns>List of stock movements within the specified date range</returns>
        [HttpGet("{productId}/movements/by-date")]
        public async Task<ActionResult<IEnumerable<StockDto>>> GetStockMovementsByDateRangeAsync([FromRoute] Guid productId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
                var stockMovements = await _stockService.GetStockMovementByDateRangeAsync(productId, start, end);
                return Ok(stockMovements);
        }
    }
}
