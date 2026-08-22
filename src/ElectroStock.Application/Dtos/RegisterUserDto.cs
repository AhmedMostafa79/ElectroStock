using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Request payload used to register a new user account.
    /// </summary>
    public class RegisterUserDto
    {
        /// <summary>
        /// First name of the new user.
        /// </summary>
        [Required]
        public string FirstName { get;  set; }

        /// <summary>
        /// Last name of the new user.
        /// </summary>
        [Required]
        public string LastName { get;  set; }

        /// <summary>
        /// Mobile number used as the primary contact and account identifier.
        /// </summary>
        [Required]
        public string PhoneNumber { get;  set; }

        /// <summary>
        /// Unique email address of the user.
        /// </summary>
        [Required]
        public string Email { get; set; }

        /// <summary>
        /// Plain-text password that will be securely hashed before persistence.
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Role assigned to the user at creation time.
        /// </summary>
        [Required]
        public UserRole Role { get;  set; }
    }
}
