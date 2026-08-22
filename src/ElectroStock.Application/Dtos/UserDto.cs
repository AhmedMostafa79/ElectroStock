using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Dtos
{
    /// <summary>
    /// Response model containing the public profile and role information for a user.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Unique database identifier of the user.
        /// </summary>
        public Guid Id { get;  init; }

        /// <summary>
        /// User first name.
        /// </summary>
        public string FirstName { get; init; }

        /// <summary>
        /// User last name.
        /// </summary>
        public string LastName { get; init; }

        /// <summary>
        /// User phone number.
        /// </summary>
        public string PhoneNumber { get; init; }

        /// <summary>
        /// Email address for registered users; may be null for guest accounts.
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// Current active status of the user account; may be null for guest accounts.
        /// </summary>
        public UserStatus? UserStatus { get; init; }

        /// <summary>
        /// Role assigned to this user.
        /// </summary>
        public UserRole Role { get; init; }

        /// <summary>
        /// User type, such as guest or registered account.
        /// </summary>
        public UserType UserType { get; init; }

        /// <summary>
        /// Timestamp when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
    }
}
