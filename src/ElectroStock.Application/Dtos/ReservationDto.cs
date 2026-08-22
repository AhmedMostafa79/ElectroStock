using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Response model returned for reservation creation or retrieval.
    /// It describes the reservation state and sale details.
    /// </summary>
    public class ReservationDto
    {
        /// <summary>
        /// Database identifier of the reservation.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Unique reservation number assigned to this reservation.
        /// </summary>
        public string ReservationNumber { get; init ; }

        /// <summary>
        /// Product reserved by the customer.
        /// </summary>
        public Guid ProductId { get; init ; }

        /// <summary>
        /// Customer associated with the reservation.
        /// </summary>
        public Guid CustomerId { get; init ; }

        /// <summary>
        /// Quantity reserved.
        /// </summary>
        public int Quantity { get; init ; }

        /// <summary>
        /// Unit price used for the reservation.
        /// </summary>
        public decimal UnitPrice { get; init ; }

        /// <summary>
        /// Total value for the reserved quantity.
        /// </summary>
        public decimal TotalPrice { get; init ; }

        /// <summary>
        /// Source channel that created the reservation.
        /// </summary>
        public RequestSource Source { get; init ; }

        /// <summary>
        /// Current reservation status, such as active or confirmed.
        /// </summary>
        public ReservationStatus Status { get; init ; }

        /// <summary>
        /// Timestamp when the reservation was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Expiration time after which the reservation should no longer be valid.
        /// </summary>
        public DateTime ExpiresAt { get; init; }
    }
}
