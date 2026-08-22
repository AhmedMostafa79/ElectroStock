using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Defines reservation lifecycle operations for customer product holds and conversion to sale.
    /// </summary>
    public interface IReservationService
    {
        /// <summary>
        /// Creates a reservation for a product and holds the requested stock.
        /// </summary>
        /// <param name="reservationDto">The reservation details and product request.</param>
        /// <param name="performedBy">The user who initiated the reservation.</param>
        /// <returns>The created reservation DTO.</returns>
        public Task<ReservationDto> CreateReservationAsync(CreateReservationDto reservationDto, Guid performedBy);

        /// <summary>
        /// Retrieves a reservation by its generated reservation number.
        /// </summary>
        /// <param name="reservationNumber">The reservation number.</param>
        /// <returns>The matching reservation DTO.</returns>
        public Task<ReservationDto> GetReservationByNumberAsync(string reservationNumber);

        /// <summary>
        /// Retrieves all reservations for a customer.
        /// </summary>
        /// <param name="customerId">The customer id.</param>
        /// <returns>Reservation DTOs for that customer.</returns>
        public Task<IEnumerable<ReservationDto>> GetByCustomerAsync(Guid customerId);

        /// <summary>
        /// Retrieves reservations for products matching the supplied SKU.
        /// </summary>
        /// <param name="sku">The product SKU.</param>
        /// <returns>Matching reservation DTOs.</returns>
        public Task<IEnumerable<ReservationDto>> GetByProductSkuAsync(string sku);

        /// <summary>
        /// Retrieves all reservations for a product id.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <returns>Matching reservation DTOs.</returns>
        public Task<IEnumerable<ReservationDto>> GetByProductIdAsync(Guid productId);

        /// <summary>
        /// Confirms a reservation and converts it into a payment-based sale flow.
        /// </summary>
        /// <param name="reservationNumber">The reservation number.</param>
        /// <param name="paymentMethod">The payment method used for confirmation.</param>
        /// <param name="performedBy">The user who confirmed the reservation.</param>
        /// <returns>The confirmed reservation DTO.</returns>
        public Task<ReservationDto> ConfirmReservationAsync(string reservationNumber, PaymentMethod paymentMethod, Guid performedBy);

        /// <summary>
        /// Cancels an active reservation and releases the held stock.
        /// </summary>
        /// <param name="reservationNumber">The reservation number.</param>
        /// <param name="performedBy">The user who cancelled the reservation.</param>
        /// <returns>The updated reservation DTO.</returns>
        public Task<ReservationDto> CancelReservationAsync(string reservationNumber, Guid performedBy);

        /// <summary>
        /// Expires an active reservation after its validity period has passed.
        /// </summary>
        /// <param name="reservationNumber">The reservation number.</param>
        /// <param name="performedBy">The user who processed the expiration.</param>
        /// <returns>The expired reservation DTO.</returns>
        public Task<ReservationDto> ExpireReservationAsync(string reservationNumber, Guid performedBy);
    }
}
