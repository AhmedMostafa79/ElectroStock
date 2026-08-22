using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Single line item included in an invoice creation request.
    /// </summary>
    public class AddInvoiceItemDto
    {
        /// <summary>
        /// Product being sold in this invoice item.
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Optional invoice number, primarily used when adding an item to an existing invoice.
        /// </summary>
        [Required]
        public string? InvoiceNumber { get; set; } =null;

        /// <summary>
        /// Quantity being sold for the product.
        /// </summary>
        [Required]
        public int Quantity { get; set; } = 1;
    }
}
