using System.Security.Claims;

namespace ElectroStock.API.Helpers
{
    /// <summary>
    /// Helper class for extracting current user information from HttpContext.
    /// </summary>
    public static class CurrentUserHelper
    {
        /// <summary>
        /// Gets the authenticated user's ID from the HttpContext.
        /// </summary>
        /// <param name="user">The ClaimsPrincipal from HttpContext.User</param>
        /// <returns>The GUID of the authenticated user</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated</exception>
        public static Guid GetCurrentUserId(ClaimsPrincipal user)
        {
            if (user == null)
                throw new UnauthorizedAccessException("User not authenticated");

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated");

            return Guid.Parse(userId);
        }
    }
}
