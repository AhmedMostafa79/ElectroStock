using ElectroStock.Application.Dtos;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Defines invoice creation, cancellation, and lookup operations.
    /// </summary>
    public interface IInvoiceService
    {
        /// <summary>
        /// Creates a new invoice for a customer and deducts the corresponding stock quantities.
        /// </summary>
        /// <param name="invoiceDto">The customer, payment, and item data for the invoice.</param>
        /// <param name="performedBy">The user who initiated the invoice creation.</param>
        /// <returns>The created invoice DTO.</returns>
        public Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto invoiceDto, Guid performedBy);

        /// <summary>
        /// Creates an invoice from an already confirmed reservation.
        /// </summary>
        /// <param name="reservationDto">The reservation-based invoice data.</param>
        /// <param name="performedBy">The user who initiated the invoice creation.</param>
        /// <returns>The created invoice DTO.</returns>
        public Task<InvoiceDto> CreateReservationInvoiceAsync(CreateReservationInvoiceDto reservationDto, Guid performedBy);

        /// <summary>
        /// Removes a product line item from an invoice and refunds the corresponding stock quantity.
        /// </summary>
        /// <param name="invoiceNumber">The invoice number.</param>
        /// <param name="productId">The product id to remove from the invoice.</param>
        /// <param name="performedBy">The user who initiated the change.</param>
        /// <returns>The updated invoice DTO.</returns>
        public Task<InvoiceDto> RemoveItemFromInvoiceAsync(string invoiceNumber, Guid productId, Guid performedBy);

        /// <summary>
        /// Retrieves an invoice by its generated invoice number.
        /// </summary>
        /// <param name="invoiceNumber">The invoice number.</param>
        /// <returns>The matching invoice DTO.</returns>
        public Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber);

        /// <summary>
        /// Retrieves an invoice by its database id.
        /// </summary>
        /// <param name="invoiceId">The invoice id.</param>
        /// <returns>The matching invoice DTO.</returns>
        public Task<InvoiceDto> GetInvoiceByIdAsync(Guid invoiceId);

        /// <summary>
        /// Retrieves all invoices for the supplied customer.
        /// </summary>
        /// <param name="customerId">The customer id.</param>
        /// <returns>All invoice DTOs for the customer.</returns>
        public Task<IEnumerable<InvoiceDto>> GetInvoicesByCustomerAsync(Guid customerId);

        /// <summary>
        /// Cancels an existing invoice and returns the sold stock quantities to inventory.
        /// </summary>
        /// <param name="invoiceNumber">The invoice number.</param>
        /// <param name="performedBy">The user who initiated the cancellation.</param>
        public Task CancelInvoiceAsync(string invoiceNumber, Guid performedBy);
    }
}
