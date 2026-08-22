using ElectroStock.Application.Dtos;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Defines authentication operations for validating user credentials and issuing tokens.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user and returns a signed access token when the account is valid and active.
        /// </summary>
        /// <param name="model">The email and password login request.</param>
        /// <returns>A JWT authentication response payload.</returns>
        public Task<AuthResponseModel> LoginAsync(AuthRequestModel model);
    }
}
