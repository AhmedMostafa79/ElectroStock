using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines product-specific query operations in addition to generic repository behavior.
    /// </summary>
    public interface IProductRepository: IRepository<Product>
    {
        /// <summary>
        /// Retrieves products by category.
        /// </summary>
        /// <param name="category">Product category filter.</param>
        /// <returns>Products matching the requested category.</returns>
        public Task<IEnumerable<Product>> GetByCategoryAsync(ProductCategory category);

        /// <summary>
        /// Retrieves products sharing the same SKU.
        /// </summary>
        /// <param name="sku">Stock keeping unit identifier.</param>
        /// <returns>Products mapped to the supplied SKU.</returns>
        public Task<IEnumerable<Product>> GetBySkuAsync(string sku);

    }
}
