using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Response model returned after invoice creation or retrieval.
    /// It represents the invoice and its line items for the client.
    /// </summary>
    public class InvoiceDto
    {
        /// <summary>
        /// Database identifier for the invoice.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Unique invoice number generated for the invoice.
        /// </summary>
        public string InvoiceNumber { get; init; }

        /// <summary>
        /// Customer id associated with the invoice.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Line items included in the invoice.
        /// </summary>
        public IEnumerable<InvoiceItem> Items { get; init; }

        /// <summary>
        /// Total value of the invoice.
        /// </summary>
        public decimal TotalPrice { get; init; }

        /// <summary>
        /// Source channel that created the invoice.
        /// </summary>
        public RequestSource OrderSource { get; init; }

        /// <summary>
        /// User responsible for creating the invoice.
        /// </summary>
        [Required]
        public Guid CreatedBy { get; init; }

        /// <summary>
        /// Payment method chosen for the invoice.
        /// </summary>
        [Required]
        public PaymentMethod PaymentMethod { get; init; }

        /// <summary>
        /// Date and time when the invoice was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; init; }
    }
}
