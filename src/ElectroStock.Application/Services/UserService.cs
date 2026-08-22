using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;

namespace ElectroStock.Application.Services
{
    public class UserService: IUserService
    {
        private readonly IUserRepository _usersRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLogService;
        public UserService(IUserRepository usersRepo, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IAuditLogService auditLogService)
        {
            _usersRepo = usersRepo;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        // Keeps the user insert and audit log in the same transaction so failures do not leave partial state.
        public async Task<UserDto> CreateStaffUserAsync(RegisterUserDto userDto,Guid performedBy)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                
                var user = new RegisteredUser(
                        data: userDto.ToCreationData(),
                        email:userDto.Email,
                        passwordHash: _passwordHasher.HashPassword(userDto.Password)
                    );

                await _usersRepo.AddAsync(user);

                await _auditLogService.LogAuditAsync(
                    performedBy: performedBy,
                    referenceType: AuditLogType.StaffUserCreation,
                    details: $"Created staff user successfully with email: {userDto.Email}, performed by: {performedBy}",
                    isInTransaction: true);

                await _unitOfWork.CommitTransactionAsync();
                return user.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
                   referenceType: AuditLogType.StaffUserCreation,
                   details: $"Failed to create staff user with email: {userDto.Email}, performed by: {performedBy}",
                   isInTransaction: false);
                
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                await _auditLogService.LogAuditAsync(
                   performedBy: performedBy,
                   referenceType: AuditLogType.StaffUserCreation,
                   details: $"Failed to create staff user with email: {userDto.Email}, performed by: {performedBy}",
                   isInTransaction: false);

                throw new Exception($"Failed to create staff account for user with phone: {userDto.PhoneNumber}", ex);
            }
        }
        public async Task<UserDto> GetOrCreateGuestUserAsync(string firstName, string lastName, string phoneNumber)
        {
            try
            {

                var user = await _usersRepo.GetByPhoneAsync(phoneNumber);
                if (user == null)
                {
                    user=new GuestUser(
                            new UserCreationData
                            {
                                FirstName = firstName,
                                LastName = lastName,
                                PhoneNumber = phoneNumber,
                                Role = UserRole.Customer
                            }
                        );
                    await _usersRepo.AddAsync( user );
                    await _unitOfWork.SaveChangesAsync();
                }

                return user.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex) {
                throw new Exception($"Failed to get or create customer account for user with phone: {phoneNumber}",ex);
            }
        }
       
        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var user = await _usersRepo.GetByIdAsync(userId);

                if (user == null)
                    throw new KeyNotFoundException($"User with id: {userId} is not found!");

                return user.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve account for user with id: {userId}",ex);
            }
        }
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _usersRepo.GetByEmailAsync(email);

                if (user == null)
                    throw new KeyNotFoundException($"User with email: {email} is not found!");

                return user.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve account for user with email: {email}",ex);
            }
        } 
        public async Task<UserDto> GetUserByPhoneAsync(string phone)
        {
            try
            {
                var user = await _usersRepo.GetByPhoneAsync(phone);

                if (user == null)
                    throw new KeyNotFoundException($"User with phone: {phone} is not found!");

                return user.ToDto();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve account for user with phone: {phone}", ex);
            }
        }
        public async Task<IEnumerable<UserDto>> GetAllUsersByRoleAsync(UserRole userRole)
        {
            try
            {
                var users = await _usersRepo.GetAllByRoleAsync(userRole);


                return users.Select(u=>u.ToDto());

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve users with role: {userRole}", ex);
            }
        }public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _usersRepo.GetAllAsync();


                return users.Select(u=>u.ToDto());

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve all users!", ex);
            }
        }

        public async Task ActivateUserAsync(string email)
        {
            try
            {
                var user = await _usersRepo.GetByEmailAsync(email);
               
                if (user == null)
                    throw new KeyNotFoundException($"User with email: {email} is not found!");

                var registeredUser = (RegisteredUser)user;

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                registeredUser.Activate();
                _usersRepo.Update(registeredUser);

                await _unitOfWork.SaveChangesAsync();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to activate account for user with email: {email}",ex);
            }
        }
        public async Task DeactivateUserAsync(string email)
        {
            try
            {
                var user = await _usersRepo.GetByEmailAsync(email);
                
                if (user == null)
                    throw new KeyNotFoundException($"User with email: {email} is not found!");

                var registeredUser = (RegisteredUser)user;

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                registeredUser.Deactivate();
                _usersRepo.Update(registeredUser);

                await _unitOfWork.SaveChangesAsync();

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deactivate account for user with email: {email}", ex);
            }
        }

        // Validates the current password before changing the stored hash.
        public async Task<UserDto> ChangePasswordAsync( Guid userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _usersRepo.GetByIdAsync(userId);

                if (user == null)
                    throw new KeyNotFoundException($"User with id: {userId} is not found!");

                if(user.UserType!=UserType.Registered)
                    throw new InvalidOperationException($"Can't change password for non-registered user, user id {userId}");

                var registeredUser= (RegisteredUser)user;
                
                if (!_passwordHasher.VerifyPassword(oldPassword, registeredUser.PasswordHash))
                    throw new InvalidOperationException("Error!: wrong password!");

                // explicitly called update, not depending on GetByNumberAsync(), as this shouldn't know about changeTracker 
                registeredUser.ChangePassword(_passwordHasher.HashPassword(newPassword));

                _usersRepo.Update(registeredUser);
                await _unitOfWork.SaveChangesAsync();

                return registeredUser.ToDto();
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to change password for user with id: {userId}", ex);
            }
        }
      
    }
}
