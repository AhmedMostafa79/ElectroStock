using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Login request used to authenticate a registered user and receive a JWT token.
    /// </summary>
    public class AuthRequestModel
    {
        /// <summary>
        /// Registered user email address used for login.
        /// </summary>
        [Required]
        public string Email { get; set; }

        /// <summary>
        /// User password for authentication.
        /// </summary>
        [Required]
        public string Password { get; set; }
    }
}
