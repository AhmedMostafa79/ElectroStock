using ElectroStock.API.Helpers;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles user profile and account management operations.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for authenticated users to view and manage their profile.
    /// All endpoints require authentication (any role: Customer, Cashier, Warehouse, Admin).
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class UserController:ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService=userService;
        }
       
        /// <summary>
        /// Retrieves the authenticated user's profile information.
        /// </summary>
        /// <remarks>
        /// Gets detailed profile information for the currently authenticated user.
        /// All authenticated users (any role) are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/user/profile
        /// Authorization: Bearer {token}
        /// 
        /// </remarks>
        /// <returns>The authenticated user's profile information</returns>
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfileAsync()
        {
            var userId = CurrentUserHelper.GetCurrentUserId(User);
            var user = await _userService.GetUserByIdAsync(userId);
                return Ok(user);
        }
        /// <summary>
        /// Changes the authenticated user's password.
        /// </summary>
        /// <remarks>
        /// Updates the password for the currently authenticated user.
        /// Requires both old and new passwords. The new password must meet security requirements.
        /// All authenticated users (any role) are authorized to perform this action.
        /// 
        /// Sample request:
        /// PUT /api/user/change-password
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "oldPassword": "OldPass123!",
        ///   "newPassword": "NewSecurePass456!"
        /// }
        /// 
        /// </remarks>
        /// <param name="request">The password change request containing old and new passwords</param>
        /// <returns>Success message confirming the password change</returns>
        [HttpPut("change-password")]
        public async Task<ActionResult<UserDto>> ChangePasswordAsync([FromBody]ChangePasswordRequest request)
        {
            var userId = CurrentUserHelper.GetCurrentUserId(User);
            var user =await _userService.ChangePasswordAsync(userId,request.OldPassword,request.NewPassword);
                return Ok(user);
        }
    }
}
