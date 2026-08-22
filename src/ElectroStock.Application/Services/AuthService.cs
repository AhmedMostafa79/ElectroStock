using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;

namespace ElectroStock.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _usersRepo;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(ITokenService tokenService, 
            IUserRepository usersRepo, IPasswordHasher passwordHasher)
        {
            _tokenService = tokenService;
            _usersRepo = usersRepo;
            _passwordHasher = passwordHasher;
        }
        // Only active accounts receive JWTs, even if the password is valid.
        public async Task<AuthResponseModel> LoginAsync(AuthRequestModel model)
        {
            try
            {
                var user=await _usersRepo.GetByEmailAsync(model.Email);
                
                if(user==null)
                    throw new UnauthorizedAccessException("Invalid email or password!");
               
                if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                    throw new UnauthorizedAccessException("Invalid email or password!");

                if (user.Status != UserStatus.Active)
                    throw new UnauthorizedAccessException("Account is not active. Please contact administrator.");

                var token = _tokenService.GetToken(user.ToDto());

                return new AuthResponseModel
                {
                    UserId=user.Id,
                    Email=model.Email,
                    ExpiresIn=token.ExpiresAt,
                    IsAuthenticated=true,
                    Role= user.Role,
                    Token=token.Token,
                    UserName = $"{user.FirstName} {user.LastName}"
                };
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to login user with email {model.Email}", ex);
            }
        }

       
    }
}
