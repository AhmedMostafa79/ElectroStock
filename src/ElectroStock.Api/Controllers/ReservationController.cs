using ElectroStock.API.Helpers;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles reservation-related operations including creation, retrieval, confirmation, and cancellation.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for cashiers and administrators to manage product reservations.
    /// All endpoints require authentication with Cashier or Admin role.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cashier,Admin")]
    [ApiConventionType(typeof(DefaultApiConventions))]

    public class ReservationController:ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }
        /// <summary>
        /// Creates a new reservation for a single product.
        /// </summary>
        /// <remarks>
        /// Creates a new reservation for a customer, reserving stock for a single product (MVP design).
        /// Stock is reserved but not yet deducted from inventory.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/reservation
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "quantity": 2,
        ///   "firstName": "John",
        ///   "lastName": "Doe",
        ///   "phoneNumber": "01234567890",
        ///   "source": "InStore"
        /// }
        /// </remarks>
        /// <param name="reservationDto">The reservation creation details including product, quantity, and customer info</param>
        /// <returns>The newly created reservation with calculated totals</returns>
        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservationAsync([FromBody] CreateReservationDto reservationDto)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var reservation = await _reservationService.CreateReservationAsync(reservationDto, performedBy);
                return Ok(reservation);
        }

        /// <summary>
        /// Retrieves a reservation by its reservation number.
        /// </summary>
        /// <remarks>
        /// Gets detailed information about a specific reservation including all items and status.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/reservation/RES-20250724-0001
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="reservationNumber">The unique reservation number (e.g., RES-20250724-0001)</param>
        /// <returns>Detailed reservation information including items and status</returns>
        [HttpGet("{reservationNumber}")]
        public async Task<ActionResult<ReservationDto>> GetReservationByNumberAsync([FromRoute]string reservationNumber)
        {
                var reservation = await _reservationService.GetReservationByNumberAsync(reservationNumber);
                return Ok(reservation);
        }

        /// <summary>
        /// Retrieves all reservations for a specific customer.
        /// </summary>
        /// <remarks>
        /// Gets a list of all reservations belonging to a customer, ordered by creation date.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/reservation/customer/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="customerId">The unique identifier (GUID) of the customer</param>
        /// <returns>List of reservations associated with the customer</returns>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByCustomerAsync([FromRoute]Guid customerId)
        {
                var reservations = await _reservationService.GetByCustomerAsync(customerId);
                return Ok(reservations);
        }

        /// <summary>
        /// Retrieves all reservations for a specific product SKU.
        /// </summary>
        /// <remarks>
        /// Gets a list of all reservations that include a specific product SKU.
        /// This is useful for tracking which customers have reserved a particular product.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/reservation/product/sku/CBL-USBC-001
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="sku">The SKU of the product to search for</param>
        /// <returns>List of reservations containing the specified product SKU</returns>
        [HttpGet("product/sku/{sku}")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByProductSkuAsync([FromRoute] string sku)
        {
                var reservations = await _reservationService.GetByProductSkuAsync(sku);
                return Ok(reservations);
        }

        /// <summary>
        /// Confirms a reservation and converts it to an invoice.
        /// </summary>
        /// <remarks>
        /// Confirms a pending reservation, converting it to a confirmed invoice.
        /// Stock is permanently deducted from inventory and the customer is charged.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// PATCH /api/reservation/RES-20250724-0001?method=Cash
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="reservationNumber">The reservation number to confirm</param>
        /// <param name="method">The payment method (Cash, DebitCard)</param>
        /// <returns>The confirmed invoice details</returns>
        [HttpPatch("{reservationNumber}")]
        public async Task<ActionResult<ReservationDto>> ConfirmReservationAsync([FromRoute] string reservationNumber,[FromQuery]PaymentMethod method)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var reservation = await _reservationService.ConfirmReservationAsync(reservationNumber, method, performedBy);
                return Ok(reservation);
        }

        /// <summary>
        /// Cancels a pending reservation.
        /// </summary>
        /// <remarks>
        /// Cancels a pending reservation, releasing all reserved stock back to inventory.
        /// Only pending reservations can be cancelled.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/reservation/RES-20250724-0001/cancel
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="reservationNumber">The reservation number to cancel</param>
        /// <returns>The updated reservation with status set to Cancelled</returns>
        [HttpPost("{reservationNumber}/cancel")]
        public async Task<ActionResult<ReservationDto>> CancelReservationAsync([FromRoute] string reservationNumber)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var reservation = await _reservationService.CancelReservationAsync(reservationNumber, performedBy);
                return Ok(reservation);
        }
    }
}
