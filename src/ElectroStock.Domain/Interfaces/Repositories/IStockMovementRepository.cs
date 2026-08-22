using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines persistence and query operations for stock movement history entries.
    /// </summary>
    public interface IStockMovementRepository
    {
        /// <summary>
        /// Adds a stock movement entry to the current persistence context.
        /// </summary>
        /// <param name="stockMovement">Stock movement entry to persist.</param>
        public Task AddAsync(StockMovement stockMovement);

        /// <summary>
        /// Retrieves stock movements for a product.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>Stock movement history entries for the product.</returns>
        public Task<IEnumerable<StockMovement>> GetByProductIdAsync(Guid productId);

        /// <summary>
        /// Retrieves stock movements for a product within a date range.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="start">Inclusive range start.</param>
        /// <param name="end">Inclusive range end.</param>
        /// <returns>Stock movement entries in the requested range.</returns>
        public Task<IEnumerable<StockMovement>> GetByDateRangeAsync(Guid productId, DateTime start, DateTime end);
    }
}
