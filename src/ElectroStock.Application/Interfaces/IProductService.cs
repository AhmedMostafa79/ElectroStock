using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Defines the product catalog operations used by the application layer.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Creates a quantified product and optionally records the initial stock reception.
        /// </summary>
        /// <param name="productDto">The quantified product definition and initial stock values.</param>
        /// <param name="performedBy">The user who initiated the creation.</param>
        /// <returns>The created product DTO.</returns>
        public Task<ProductDto> CreateQuantifiedProductAsync(CreateQuantifiedProductDto productDto, Guid performedBy);

        /// <summary>
        /// Retrieves a product by its unique identifier.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <returns>The matching product DTO.</returns>
        public Task<ProductDto> GetProductByIdAsync(Guid productId);

        /// <summary>
        /// Retrieves all products matching the supplied SKU.
        /// </summary>
        /// <param name="sku">The SKU to search for.</param>
        /// <returns>All matching product DTOs.</returns>
        public Task<IEnumerable<ProductDto>> GetProductsBySkuAsync(string sku);

        /// <summary>
        /// Retrieves all products in the supplied category.
        /// </summary>
        /// <param name="category">The product category to filter by.</param>
        /// <returns>Matching product DTOs.</returns>
        public Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(ProductCategory category);

        /// <summary>
        /// Retrieves the full product catalog.
        /// </summary>
        /// <returns>All product DTOs.</returns>
        public Task<IEnumerable<ProductDto>> GetAllProductsAsync();

        /// <summary>
        /// Updates the selling price for all products that share the same SKU.
        /// </summary>
        /// <param name="sku">The product SKU.</param>
        /// <param name="newPrice">The replacement selling price.</param>
        /// <param name="performedBy">The user who performed the change.</param>
        public Task UpdateProductsPriceAsync(string sku, decimal newPrice, Guid performedBy);

        /// <summary>
        /// Updates the cost for all products that share the same SKU.
        /// </summary>
        /// <param name="sku">The product SKU.</param>
        /// <param name="newCost">The replacement product cost.</param>
        /// <param name="performedBy">The user who performed the change.</param>
        public Task UpdateProductsCostAsync(string sku, decimal newCost, Guid performedBy);

        /// <summary>
        /// Marks a product as discontinued and prevents it from normal active business usage.
        /// </summary>
        /// <param name="productId">The product id to discontinue.</param>
        /// <param name="performedBy">The user who performed the action.</param>
        public Task DiscontinueProductAsync(Guid productId, Guid performedBy);

        /// <summary>
        /// Restores a discontinued product to active status.
        /// </summary>
        /// <param name="productId">The product id to publish.</param>
        /// <param name="performedBy">The user who performed the action.</param>
        public Task PublishProductAsync(Guid productId, Guid performedBy);
    }
}
