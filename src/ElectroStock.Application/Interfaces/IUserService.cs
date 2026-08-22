using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Defines the user lifecycle and lookup operations for staff and customer accounts.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Creates a new registered staff user and hashes the supplied password.
        /// </summary>
        /// <param name="userDto">The registration payload.</param>
        /// <param name="performedBy">The user who initiated the account creation.</param>
        /// <returns>The created user DTO.</returns>
        public Task<UserDto> CreateStaffUserAsync(RegisterUserDto userDto, Guid performedBy);

        /// <summary>
        /// Returns an existing customer by phone number or creates a guest user if no record exists.
        /// </summary>
        /// <param name="firstName">The guest/customer first name.</param>
        /// <param name="lastName">The guest/customer last name.</param>
        /// <param name="phoneNumber">The customer phone number used for lookup.</param>
        /// <returns>The existing or newly created customer DTO.</returns>
        public Task<UserDto> GetOrCreateGuestUserAsync(string firstName, string lastName, string phoneNumber);

        /// <summary>
        /// Activates an existing user account.
        /// </summary>
        /// <param name="email">The user email address.</param>
        public Task ActivateUserAsync(string email);

        /// <summary>
        /// Deactivates an existing user account.
        /// </summary>
        /// <param name="email">The user email address.</param>
        public Task DeactivateUserAsync(string email);

        /// <summary>
        /// Retrieves a user by database id.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>The matching user DTO.</returns>
        public Task<UserDto> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Retrieves a user by email address.
        /// </summary>
        /// <param name="email">The user email address.</param>
        /// <returns>The matching user DTO.</returns>
        public Task<UserDto> GetUserByEmailAsync(string email);

        /// <summary>
        /// Retrieves a user by phone number.
        /// </summary>
        /// <param name="phone">The phone number.</param>
        /// <returns>The matching user DTO.</returns>
        public Task<UserDto> GetUserByPhoneAsync(string phone);

        /// <summary>
        /// Retrieves all users assigned to the supplied role.
        /// </summary>
        /// <param name="userRole">The role filter.</param>
        /// <returns>All users for that role.</returns>
        public Task<IEnumerable<UserDto>> GetAllUsersByRoleAsync(UserRole userRole);

        /// <summary>
        /// Retrieves all users in the system.
        /// </summary>
        /// <returns>All user DTOs.</returns>
        public Task<IEnumerable<UserDto>> GetAllUsersAsync();

        /// <summary>
        /// Changes the password for a registered user after validating the current password.
        /// </summary>
        /// <param name="userId">The user whose password will change.</param>
        /// <param name="oldPassword">The current password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns>The updated user DTO.</returns>
        public Task<UserDto> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    }
}
