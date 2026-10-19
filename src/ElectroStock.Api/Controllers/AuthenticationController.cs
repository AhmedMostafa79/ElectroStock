using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles authentication and user session management.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for user authentication, including login.
    /// All endpoints in this controller are publicly accessible (AllowAnonymous).
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class AuthenticationController:ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthenticationController(IAuthService authService)
        {
            _authService=authService;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT access token.
        /// </summary>
        /// <remarks>
        /// Validates user credentials (email + password) and returns a JWT token
        /// for subsequent authenticated API requests. The token includes user role claims
        /// for authorization purposes.
        /// 
        /// Sample request:
        /// POST /api/authentication/login
        /// Content-Type: application/json
        /// 
        /// {
        ///   "identifier": "admin@electrostock.com",
        ///   "password": "Admin547!"
        /// }
        /// 
        /// </remarks>
        /// <param name="loginRequest">The login credentials containing email and password</param>
        /// <returns>JWT token and user details upon successful authentication</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseModel>> LoginAsync([FromBody] AuthRequestModel loginRequest)
        {
                var response = await _authService.LoginAsync(loginRequest);
                return Ok(response);
        } 
       
        
    }
}
