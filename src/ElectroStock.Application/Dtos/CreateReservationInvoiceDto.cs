using ElectroStock.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Internal request model used when converting a reservation into an invoice.
    /// It contains the reservation data needed to produce the sale record.
    /// </summary>
    public class CreateReservationInvoiceDto
    {
        /// <summary>
        /// The reservation number being confirmed and closed into an invoice.
        /// </summary>
        [Required]
        public string ReservationNumber { get; set; }

        /// <summary>
        /// Customer associated with the reservation.
        /// </summary>
        [Required]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Sale channel for the invoice being generated from the reservation.
        /// </summary>
        [Required]
        public RequestSource OrderSource { get; set; }

        /// <summary>
        /// Invoice payment method selected for the confirmed reservation.
        /// </summary>
        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// Product selected in the reservation.
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity being sold as part of the reservation confirmation.
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price assigned to the product at reservation time.
        /// </summary>
        [Required]
        public decimal UnitPrice { get; set; }
    }
}
