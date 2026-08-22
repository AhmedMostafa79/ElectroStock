using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines user-specific lookup operations in addition to generic repository behavior.
    /// </summary>
    public interface IUserRepository:IRepository<User>
    {
        /// <summary>
        /// Retrieves a registered user by email.
        /// </summary>
        /// <param name="email">Email address used for lookup.</param>
        /// <returns>The matching registered user, or <c>null</c> when not found.</returns>
        public Task<RegisteredUser?> GetByEmailAsync(string email);

        /// <summary>
        /// Retrieves users filtered by role.
        /// </summary>
        /// <param name="role">Role filter.</param>
        /// <returns>Users assigned to the requested role.</returns>
        public Task<IEnumerable<User>> GetAllByRoleAsync(UserRole role);

        /// <summary>
        /// Retrieves a user by phone number.
        /// </summary>
        /// <param name="phone">Phone number used for lookup.</param>
        /// <returns>The matching user, or <c>null</c> when not found.</returns>
        public Task<User?> GetByPhoneAsync(string phone);

    }
}
