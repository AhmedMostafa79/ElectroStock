using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines reservation-specific query operations in addition to generic repository behavior.
    /// </summary>
    public interface IReservationRepository:IRepository<Reservation>
    {
        /// <summary>
        /// Retrieves reservations for a single product id.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>Reservations linked to the product.</returns>
        public Task<IEnumerable<Reservation>> GetByProductAsync(Guid productId);

        /// <summary>
        /// Retrieves a reservation by its business reservation number.
        /// </summary>
        /// <param name="reservationNumber">Generated reservation number.</param>
        /// <returns>The matching reservation, or <c>null</c> when not found.</returns>
        public Task<Reservation?> GetByNumberAsync(string reservationNumber);

        /// <summary>
        /// Retrieves reservations belonging to a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier.</param>
        /// <returns>Reservations associated with the customer.</returns>
        public Task<IEnumerable<Reservation>> GetByCustomerAsync(Guid customerId);

        /// <summary>
        /// Retrieves reservations for a set of product ids.
        /// </summary>
        /// <param name="productIds">Product identifiers to include in the query.</param>
        /// <returns>Reservations mapped to any of the supplied product ids.</returns>
        public Task<IEnumerable<Reservation>> GetByProductIdsAsync(List<Guid>productIds);

    
    }
}
