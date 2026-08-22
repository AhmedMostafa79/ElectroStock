using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Application.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using System.Data;

namespace ElectroStock.Tests.UnitTests.ApplicationLayer
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPasswordHasher> _mockHasher;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly IUserService _userService;
        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockUnitOfWork=new Mock<IUnitOfWork>();
            _mockHasher = new Mock<IPasswordHasher>();
            _mockAuditLogService=new Mock<IAuditLogService>();
            _userService = new UserService(_mockUserRepo.Object,
                _mockUnitOfWork.Object, _mockHasher.Object, _mockAuditLogService.Object);
        }
        private GuestUser CreateTestGuestUser()
        {
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };
            return new GuestUser(data);

        } 
        private RegisteredUser CreateTestRegisteredUser()
        {
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };
            string email = "abc@gmail.com";
            string passwordHash = "HashedPassword";
            return new RegisteredUser(data, email, passwordHash);
        }
        [Fact]
        public async Task CreateStaffUserAsync_WithValidData_CreatesStaffUser()
        {
            // Arrange
            var data = new RegisterUserDto
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Email = "staff@gmail.com",
                Password = "staff@123",
                Role = UserRole.Warehouse

            };
            var hashedPassword = "HashedPassword";
            var performedBy = Guid.NewGuid();

            _mockUserRepo.Setup(x => x.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockHasher.Setup(x => x.HashPassword(data.Password))
                .Returns(hashedPassword);

            _mockUnitOfWork.Setup(x=>x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask); 
           
            _mockUnitOfWork.Setup(x=>x.CommitTransactionAsync())
                .Returns(Task.CompletedTask);

            _mockAuditLogService
                .Setup(x => x.LogAuditAsync(It.IsAny<Guid>(),
                It.IsAny<AuditLogType>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
                )).Returns(Task.CompletedTask);

            // Act 
            var actualUser = await _userService.CreateStaffUserAsync(data, performedBy); ;

            // Assert
            actualUser.FirstName.Should().Be(data.FirstName);
            actualUser.LastName.Should().Be(data.LastName);
            actualUser.PhoneNumber.Should().Be(data.PhoneNumber);
            actualUser.Email.Should().Be(data.Email);
            actualUser.Role.Should().Be(data.Role);

            // Verify
            _mockAuditLogService
                          .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                          It.IsAny<AuditLogType>(),
                          It.IsAny<string>(),
                          It.IsAny<bool>()
                          ), Times.Once);

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);

        }
        [Fact]
        public async Task CreateStaffUserAsync_WhenFailsToAddUser_ThrowsException()
        {
            // Arrange
            var data = new RegisterUserDto
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Email = "staff@gmail.com",
                Password = "staff@123",
                Role = UserRole.Warehouse

            };
            var hashedPassword = "HashedPassword";
            var performedBy = Guid.NewGuid();

            _mockUserRepo.Setup(x => x.AddAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception());

            _mockHasher.Setup(x => x.HashPassword(data.Password))
                .Returns(hashedPassword);

            _mockUnitOfWork.Setup(x=>x.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                .Returns(Task.CompletedTask); 
           
            _mockUnitOfWork.Setup(x=>x.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            // Act 
            var act = async () => await _userService.CreateStaffUserAsync(data, performedBy);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            // Verify
            _mockAuditLogService
                          .Verify(x => x.LogAuditAsync(It.IsAny<Guid>(),
                          It.IsAny<AuditLogType>(),
                          It.IsAny<string>(),
                          It.IsAny<bool>()
                          ), Times.Once); // failure AuditLog

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);

        }
        [Fact]
        public async Task GetOrCreateGuestUserAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            var expectedUser = CreateTestGuestUser();
            
            _mockUserRepo.Setup(x => x.GetByPhoneAsync(expectedUser.Phone))
                .ReturnsAsync(expectedUser); 
            
           

            // Act
            var actualUser = await _userService
                .GetOrCreateGuestUserAsync(expectedUser.FirstName, expectedUser.LastName, expectedUser.Phone);

            // Assert

            actualUser.Should().BeEquivalentTo(expectedUser.ToDto());

            // Verify
            _mockUserRepo.Verify(x => x.AddAsync(expectedUser),Times.Never);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);

        }
        [Fact]
        public async Task GetOrCreateGuestUserAsync_WithNonExistingUser_CreatesGuestUser()
        {
            // Arrange
            var expectedUser = CreateTestGuestUser();
            
            _mockUserRepo.Setup(x => x.GetByPhoneAsync(expectedUser.Phone))
                .ReturnsAsync((User?)null);

            _mockUserRepo.Setup(x => x.AddAsync(expectedUser))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var actualUser = await _userService
                .GetOrCreateGuestUserAsync(expectedUser.FirstName, expectedUser.LastName, expectedUser.Phone);

            // Assert
            actualUser.FirstName.Should().Be(expectedUser.FirstName);
            actualUser.LastName.Should().Be(expectedUser.LastName);
            actualUser.PhoneNumber.Should().Be(expectedUser.Phone);

            // Verify
            _mockUserRepo.Verify(x => x.AddAsync(It.IsAny<User>()),Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);

        }
        [Fact]
        public async Task GetOrCreateGuestUserAsync_WhenFailsToCreateUser_ThrowsException()
        {
            // Arrange
            var phoneNumber = "";
            
            _mockUserRepo.Setup(x => x.GetByPhoneAsync(phoneNumber))
                .ThrowsAsync(new Exception());


            // Act
            var act = async ()=>await _userService
                .GetOrCreateGuestUserAsync("","", phoneNumber);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            // Verify
            _mockUserRepo.Verify(x => x.AddAsync(It.IsAny<User>()),Times.Never);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);

        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            var expectedUser = CreateTestGuestUser();

            _mockUserRepo.Setup(x => x.GetByIdAsync(expectedUser.Id))
                .ReturnsAsync(expectedUser);



            // Act
            var actualUser = await _userService
                .GetUserByIdAsync(expectedUser.Id);

            // Assert

            actualUser.Should().BeEquivalentTo(expectedUser.ToDto());

        }
        [Fact]
        public async Task GetUserByIdAsync_WhenUserNotFound_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var act=async ()=>await _userService.GetUserByIdAsync(userId);
            // Assert

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
        [Fact]
        public async Task GetUserByEmailAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            var expectedUser = CreateTestRegisteredUser();

            _mockUserRepo.Setup(x => x.GetByEmailAsync(expectedUser.Email))
                .ReturnsAsync(expectedUser);

            // Act
            var actualUser = await _userService
                .GetUserByEmailAsync(expectedUser.Email);

            // Assert
            actualUser.Should().BeEquivalentTo(expectedUser.ToDto());

        }
        [Fact]
        public async Task GetUserByEmailAsync_WhenUserNotFound_ThrowsException()
        {
            // Arrange
            var email = "";

            _mockUserRepo.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((RegisteredUser?)null);

            // Act
            var act=async ()=>await _userService.GetUserByEmailAsync(email);
            // Assert

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetUserByPhoneAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            var expectedUser = CreateTestRegisteredUser();

            _mockUserRepo.Setup(x => x.GetByPhoneAsync(expectedUser.Phone))
                .ReturnsAsync(expectedUser);

            // Act
            var actualUser = await _userService
                .GetUserByPhoneAsync(expectedUser.Phone);

            // Assert
            actualUser.Should().BeEquivalentTo(expectedUser.ToDto());

        }
        [Fact]
        public async Task GetAllUsersByRoleAsync_ReturnsListOfMatchingUsers()
        {
            // Arrange
            var expectedUsers = new List<User>(){
                CreateTestGuestUser(),
                CreateTestGuestUser(),
                CreateTestGuestUser(),
            };

            var role=UserRole.Customer;

            _mockUserRepo.Setup(x => x.GetAllByRoleAsync(role))
                .ReturnsAsync(expectedUsers);

            // Act
            var actualUser = await _userService
                .GetAllUsersByRoleAsync(role);

            // Assert
            var expectedUsersDtos = expectedUsers.Select(u => u.ToDto());
            actualUser.Should().BeEquivalentTo(expectedUsersDtos);
        } 
        [Fact]
        public async Task GetAllUsersAsync_ReturnsListOfAllUsers()
        {
            // Arrange
            var expectedUsers = new List<User>(){
                CreateTestGuestUser(),
                CreateTestGuestUser(),
                CreateTestGuestUser(),
                CreateTestRegisteredUser(),
                CreateTestRegisteredUser(),
                CreateTestRegisteredUser()
            };

            _mockUserRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedUsers);

            // Act
            var actualUser = await _userService
                .GetAllUsersAsync();

            // Assert
            var expectedUsersDtos = expectedUsers.Select(u => u.ToDto());
            actualUser.Should().BeEquivalentTo(expectedUsersDtos);
        }

        [Fact]
        public async Task ActivateUserAsync_WhenUserExists_ActivatesUser()
        {
            // Arrange
            var user = CreateTestRegisteredUser();

            _mockUserRepo.Setup(x=>x.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);
            
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);
                ;
            // Act
            await _userService.ActivateUserAsync(user.Email);

            // Assert & Verify
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
        [Fact]
        public async Task ActivateUserAsync_WhenUserNotFound_ThrowsException()
        {
            // Arrange
            var email = "user@gmail.com";

            _mockUserRepo.Setup(x=>x.GetByEmailAsync(email))
            .ReturnsAsync((RegisteredUser?)null);
            
            // Act
            var act=async ()=> await _userService.ActivateUserAsync(email);

            // Assert & Verify
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
        }
        [Fact]
        public async Task DeactivateUserAsync_WhenUserExists_ActivatesUser()
        {
            // Arrange
            var user = CreateTestRegisteredUser();

            _mockUserRepo.Setup(x=>x.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);
            
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);
                ;

            // Act
            await _userService.DeactivateUserAsync(user.Email);

            // Assert & Verify
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
        [Fact]
        public async Task DeactivateUserAsync_WhenUserNotFound_ThrowsException()
        {
            // Arrange
            var email = "user@gmail.com";

            _mockUserRepo.Setup(x=>x.GetByEmailAsync(email))
            .ReturnsAsync((RegisteredUser?)null);
            
            // Act
            var act=async ()=> await _userService.DeactivateUserAsync(email);

            // Assert & Verify
           await  act.Should().ThrowAsync<KeyNotFoundException>();
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
        }
        [Fact]
        public async Task ChangePasswordAsync_WithCorrectOldPassword_ChangesPassword()
        {
            // Arrange
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };
            string email = "abc@gmail.com";
            string oldPassword = "user@123";
            string oldPasswordHash = "HashedUser123";

            string newPassword = "user@567";
            string newPasswordHash = "HashedUser567";

            var user= new RegisteredUser(data, email, oldPasswordHash);
            
            _mockUserRepo.Setup(x => x.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

            _mockHasher.Setup(x => x.VerifyPassword(oldPassword, oldPasswordHash))
                .Returns(true);

            _mockHasher.Setup(x => x.HashPassword(newPassword))
                .Returns(newPasswordHash);

            
            
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _userService.ChangePasswordAsync(user.Id,oldPassword,newPassword);


            // Assert & Verify
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
            _mockUserRepo.Verify(x =>x.Update(user), Times.Once);
            _mockHasher.Verify(x => x.VerifyPassword(oldPassword, oldPasswordHash), Times.Once);
            _mockHasher.Verify(x => x.HashPassword(newPassword), Times.Once);

        }
        [Fact]
        public async Task ChangePasswordAsync_WithIncorrectOldPassword_ThrowsException()
        {
            // Arrange
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };
            string email = "abc@gmail.com";
            string oldPassword = "user@123";
            string oldPasswordHash = "HashedUser123";

            string newPassword = "user@567";
            string newPasswordHash = "HashedUser567";

            var user = new RegisteredUser(data, email, oldPasswordHash);

            _mockUserRepo.Setup(x => x.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

            _mockHasher.Setup(x => x.VerifyPassword(oldPassword, oldPasswordHash))
                .Returns(false);


            _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var act=async  ()=>await _userService.ChangePasswordAsync(user.Id, oldPassword, newPassword);


            // Assert 
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Verify
            _mockHasher.Verify(x => x.VerifyPassword(oldPassword, oldPasswordHash), Times.Once);
            _mockUserRepo.Verify(x => x.Update(user), Times.Never);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
            _mockHasher.Verify(x => x.HashPassword(newPassword), Times.Never);

        }
    }
}
