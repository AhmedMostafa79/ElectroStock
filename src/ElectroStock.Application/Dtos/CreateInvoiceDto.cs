using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Request payload used to create a new invoice for a customer.
    /// The customer may be created automatically if they are not already registered.
    /// </summary>
    public class CreateInvoiceDto
    {
        /// <summary>
        /// Customer first name used to create or resolve the guest customer record.
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// Customer last name used to create or resolve the guest customer record.
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// Customer phone number used to match or create the customer record.
        /// </summary>
        [Required]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Channel through which the sale was initiated, such as in-store or online.
        /// </summary>
        [Required]
        public RequestSource OrderSource { get; set; }

        /// <summary>
        /// Payment method selected for the invoice.
        /// </summary>
        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// One or more products being sold in this invoice.
        /// </summary>
        [Required]
        public IEnumerable<AddInvoiceItemDto> Items {  get; set; }
    }
}
