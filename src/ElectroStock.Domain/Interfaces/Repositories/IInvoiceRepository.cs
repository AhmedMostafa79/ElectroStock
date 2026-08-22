using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines invoice-specific query operations in addition to generic repository behavior.
    /// </summary>
    public interface IInvoiceRepository:IRepository<Invoice>
    {
        /// <summary>
        /// Retrieves all invoices belonging to a customer.
        /// </summary>
        /// <param name="customerId">Customer identifier.</param>
        /// <returns>Invoices associated with the customer.</returns>
        public Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId);

        /// <summary>
        /// Retrieves an invoice by its business invoice number.
        /// </summary>
        /// <param name="invoiceNumber">Generated invoice number.</param>
        /// <returns>The matching invoice, or <c>null</c> when not found.</returns>
        public Task<Invoice?> GetByNumberAsync(string invoiceNumber);

    }
}
