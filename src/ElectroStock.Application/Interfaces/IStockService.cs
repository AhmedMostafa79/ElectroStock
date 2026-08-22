using ElectroStock.Application.Dtos;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Defines stock movement and availability operations for products in inventory.
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// Receives a serialized product into inventory and records the stock movement.
        /// </summary>
        /// <param name="sku">The product SKU.</param>
        /// <param name="productDto">The serialized product payload.</param>
        /// <param name="performedBy">The user who processed the receipt.</param>
        /// <returns>The created product DTO.</returns>
        public Task<ProductDto> ReceiveSerializedProductAsync(string sku, CreateSerializedProductDto productDto, Guid performedBy);

        /// <summary>
        /// Increases available stock for a quantified product and records the movement.
        /// </summary>
        /// <param name="sku">The product SKU.</param>
        /// <param name="quantity">The quantity received.</param>
        /// <param name="performedBy">The user who processed the receipt.</param>
        /// <param name="isInTransaction">When true, the method assumes a surrounding transaction is already active.</param>
        /// <returns>The updated stock summary.</returns>
        public Task<StockDto> ReceiveQuantifiedProductAsync(string sku, int quantity, Guid performedBy, bool isInTransaction = false);

        /// <summary>
        /// Reserves product stock for a pending sale or order.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="performedBy">The user who initiated the reservation.</param>
        /// <param name="quantity">The quantity to reserve.</param>
        public Task ReserveStockAsync(Guid productId, Guid performedBy, int quantity = 1);

        /// <summary>
        /// Confirms a sale and reduces available inventory for the supplied quantity.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="performedBy">The user confirming the sale.</param>
        /// <param name="quantity">The quantity sold.</param>
        /// <returns>The sale summary.</returns>
        public Task<SaleDto> ConfirmSaleAsync(Guid productId, Guid performedBy, int quantity = 1);

        /// <summary>
        /// Confirms a sale for a product that was previously reserved.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="performedBy">The user confirming the reserved sale.</param>
        /// <param name="quantity">The quantity to confirm.</param>
        /// <returns>The sale summary.</returns>
        public Task<SaleDto> ConfirmReservedSaleAsync(Guid productId, Guid performedBy, int quantity = 1);

        /// <summary>
        /// Releases a reserved quantity back into available stock.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="quantity">The quantity to release.</param>
        public Task ReleaseReservedProductAsync(Guid productId, int quantity);

        /// <summary>
        /// Restores previously sold quantity back into inventory and records the movement.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="quantity">The quantity to refund.</param>
        /// <param name="performedBy">The user who processed the refund.</param>
        public Task RefundProductAsync(Guid productId, int quantity, Guid performedBy);

        /// <summary>
        /// Returns the current stock summary for the supplied product SKU.
        /// </summary>
        /// <param name="sku">The product SKU.</param>
        /// <returns>The current stock status.</returns>
        public Task<StockDto> GetCurrentStockAsync(string sku);

        /// <summary>
        /// Retrieves the movement history for a product.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <returns>All stock movement DTOs for the product.</returns>
        public Task<IEnumerable<StockMovementDto>> GetStockMovementHistoryAsync(Guid productId);

        /// <summary>
        /// Retrieves stock movement entries for a product within a date range.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="start">The inclusive start timestamp.</param>
        /// <param name="end">The inclusive end timestamp.</param>
        /// <returns>Matching stock movement DTOs.</returns>
        public Task<IEnumerable<StockMovementDto>> GetStockMovementByDateRangeAsync(Guid productId, DateTime start, DateTime end);
    }
}
