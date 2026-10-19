using ElectroStock.API.Helpers;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles administrative operations including user management, activation, and role-based administration.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for administrators to manage users and staff accounts.
    /// All endpoints require authentication with Admin role.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        
        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Creates a new staff user account.
        /// </summary>
        /// <remarks>
        /// Creates a new staff member (Cashier, Warehouse) with the provided registration details.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/admin/staff-creation
        /// Authorization: Bearer {admin-token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "email": "cashier@electrostock.com",
        ///   "password": "SecurePass123!",
        ///   "firstName": "Jane",
        ///   "lastName": "Doe",
        ///   "phoneNumber": "01234567890",
        ///   "dateOfBirth": "1995-05-15",
        ///   "userRole": "Cashier"
        /// }
        /// </remarks>
        /// <param name="userDto">Staff member registration details (Email, Password, FirstName, LastName, PhoneNumber, DateOfBirth, UserRole)</param>
        /// <returns>Newly created staff member details</returns>
        [HttpPost("staff-creation")]
        public async Task<ActionResult<UserDto>> CreateStaffUserAsync([FromBody] RegisterUserDto userDto)
        {


            var performedBy = CurrentUserHelper.GetCurrentUserId(User);

                var user = await _userService.CreateStaffUserAsync(
                    userDto: userDto,
                    performedBy: performedBy);

                return Ok(user);
        }
        /// <summary>
        /// Activates a user account by email address.
        /// </summary>
        /// <remarks>
        /// Sets the user's account status to Active, allowing them to log in.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/admin/activation/john.doe@example.com
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="userEmail">The email address of the user to activate</param>
        /// <returns>Success message confirming activation</returns>
      
        [HttpPost("activation/{userEmail}")]
        public async Task<IActionResult> ActivateUserAsync([FromRoute] string userEmail)
        {

                await _userService.ActivateUserAsync(userEmail);
                return Ok(
                    new
                    {
                        message = $"User with email {userEmail} activated successfully"
                    });
          
        }

        /// <summary>
        /// Deactivates a user account by email address.
        /// </summary>
        /// <remarks>
        /// Sets the user's account status to Inactive, preventing them from logging in.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/admin/deactivation/john.doe@example.com
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="userEmail">The email address of the user to deactivate</param>
        /// <returns>Success message confirming deactivation</returns>
        [HttpPost("deactivation/{userEmail}")]
        public async Task<IActionResult> DeactivateUserAsync([FromRoute] string userEmail)
        {

            await _userService.DeactivateUserAsync(userEmail);
            return Ok(
                new
                {
                    message = $"User with email {userEmail} deactivated successfully"
                });
        }

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <remarks>
        /// Gets detailed information about a specific user using their GUID.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/admin/by-id/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="userId">The unique identifier (GUID) of the user to retrieve</param>
        /// <returns>Detailed user information including role and status</returns>
        [HttpGet("by-id/{userId}")]
        public async Task<ActionResult<UserDto>> GetUserByIdAsync([FromRoute] Guid userId)
        {

                var user = await _userService.GetUserByIdAsync(userId);
                return Ok(user);
           
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <remarks>
        /// Gets detailed information about a specific user using their email.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/admin/by-email/john.doe@example.com
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="email">The email address of the user to retrieve</param>
        /// <returns>Detailed user information including role and status</returns>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserDto>> GetUserByEmailAsync([FromRoute]string email)
        {

                var user = await _userService.GetUserByEmailAsync(email);
                return Ok(user);
        }

        /// <summary>
        /// Retrieves a user by their phone number.
        /// </summary>
        /// <remarks>
        /// Gets detailed information about a specific user using their phone number.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/admin/by-phone/01234567890
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="phone">The phone number of the user to retrieve</param>
        /// <returns>Detailed user information including role and status</returns>
        [HttpGet("by-phone/{phone}")]
        public async Task<ActionResult<UserDto>> GetUserByPhoneAsync([FromRoute]string phone)
        {

                var user = await _userService.GetUserByPhoneAsync(phone);
                return Ok(user);
        }

        /// <summary>
        /// Retrieves all users with a specific role.
        /// </summary>
        /// <remarks>
        /// Gets a list of all users belonging to a specific role (Customer, Cashier, Warehouse, Admin).
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/admin/users/by-role?role=Cashier
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <param name="role">The user role to filter by (Customer, Cashier, Warehouse, Admin)</param>
        /// <returns>List of users with the specified role</returns>
        [HttpGet("users/by-role")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsersByRole([FromQuery]UserRole role)
        {

            var users = await _userService.GetAllUsersByRoleAsync(role);
                return Ok(users);
        }

        /// <summary>
        /// Retrieves all users in the system.
        /// </summary>
        /// <remarks>
        /// Gets a complete list of all users regardless of role.
        /// Only users with Admin role are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/admin/users
        /// Authorization: Bearer {admin-token}
        /// </remarks>
        /// <returns>List of all users in the system</returns>
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {

                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
        }

    }
}
