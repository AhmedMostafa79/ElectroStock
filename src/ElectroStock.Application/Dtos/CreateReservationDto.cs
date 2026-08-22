using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Request model for creating a reservation against a product.
    /// </summary>
    public class CreateReservationDto
    {
        /// <summary>
        /// Product being reserved.
        /// </summary>
        [Required]
        public Guid ProductId { get;set; }

        /// <summary>
        /// Quantity requested to reserve.
        /// </summary>
        [Required]
        public int Quantity { get;set; }

        /// <summary>
        /// Customer first name used to create or resolve the guest customer record.
        /// </summary>
        [Required]
        public string FirstName {  get;set; }

        /// <summary>
        /// Customer last name used to create or resolve the guest customer record.
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// Customer phone number used to match or create the guest customer record.
        /// </summary>
        [Required]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Source channel that created the reservation.
        /// </summary>
        [Required]
        public RequestSource Source { get; set; }
    }
}
