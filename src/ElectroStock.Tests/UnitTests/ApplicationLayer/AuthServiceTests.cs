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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.UnitTests.ApplicationLayer
{
    public class AuthServiceTests
    {

        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly IAuthService _authService;

        public AuthServiceTests()
        {
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockTokenService = new Mock<ITokenService>();

            // injecting mocked services into AuthService
            _authService = new AuthService(_mockTokenService.Object,
                _mockUserRepo.Object, _mockPasswordHasher.Object);
        }
        [Fact]
        public async Task LoginAsync_WhenCredentialsValid_ReturnsResponseModel()
        {
            // Arrange
            var data = new UserCreationData
            {
                FirstName="Ahmed",
                LastName="User",
                PhoneNumber="01234567890",
                Role=UserRole.Admin,
                
            };

            var requestModel = new AuthRequestModel
            {
                Email = "ahmed@gmail.com",
                Password = "ahmed@123"
            };

            var user = new RegisteredUser(data, requestModel.Email, requestModel.Password);

            // userRepo mock setup
             _mockUserRepo.Setup(x => x.GetByEmailAsync(requestModel.Email))
                .ReturnsAsync(user);


            // password verification mock setup
            _mockPasswordHasher.Setup(x => x.VerifyPassword(requestModel.Password,user.PasswordHash))
                .Returns(true);


            //token mocking setup
            var authToken = new TokenDto
            {
                Token = "AnyToken",
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                TokenType = "AnyType"
            };

            _mockTokenService.Setup(x => x.GetToken(It.IsAny<UserDto>()))
                .Returns(authToken);

                var responseModel= new AuthResponseModel
            {
                UserId = user.Id,
                Email = requestModel.Email,
                ExpiresIn = authToken.ExpiresAt,
                IsAuthenticated = true,
                Role = user.Role,
                Token = authToken.Token,
                UserName = $"{user.FirstName} {user.LastName}"
            };


            // Act
            var response =await  _authService.LoginAsync(requestModel);


            // Assert
            response.UserId.Should().Be(responseModel.UserId);
            response.Email.Should().Be(responseModel.Email);
            response.ExpiresIn.Should().Be(responseModel.ExpiresIn);
            response.IsAuthenticated.Should().Be(responseModel.IsAuthenticated);
            response.Role.Should().Be(responseModel.Role);
            response.Token.Should().Be(responseModel.Token);
            response.UserName.Should().Be(responseModel.UserName);
        }
        [Fact]
        public async Task LoginAsync_WhenUserNotFound_ThrowsException()
        {
            // Arrange
            var requestModel = new AuthRequestModel
            {
                Email = "ahmed@gmail.com",
                Password = "ahmed@123"
            };

            // userRepo mock setup
            _mockUserRepo.Setup(x => x.GetByEmailAsync(requestModel.Email))
                .ReturnsAsync((RegisteredUser?)null);

            // Act

            var act = async () =>await  _authService.LoginAsync(requestModel);

            // Assert

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid email or password!");
        }

        [Fact]
        public async Task LoginAsync_WhenInvalidPassword_ThrowsException()
        {
            // Arrange
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };

            var requestModel = new AuthRequestModel
            {
                Email = "ahmed@gmail.com",
                Password = "ahmed@123"
            };

            var user = new RegisteredUser(data, requestModel.Email, requestModel.Password);
          
            // userRepo mock setup
            _mockUserRepo.Setup(x => x.GetByEmailAsync(requestModel.Email))
              .ReturnsAsync(user);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(requestModel.Password,user.PasswordHash))
                .Returns(false);
           

            // Act
            var act = async()=>await _authService.LoginAsync(requestModel);


            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid email or password!");
        } 
        [Fact]
        public async Task LoginAsync_WhenUserInactive_ThrowsException()
        {
            // Arrange
            var data = new UserCreationData
            {
                FirstName = "Ahmed",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Admin,

            };

            var requestModel = new AuthRequestModel
            {
                Email = "ahmed@gmail.com",
                Password = "ahmed@123"
            };

            var user = new RegisteredUser(data, requestModel.Email, requestModel.Password);
            user.Deactivate();

            // userRepo mock setup
            _mockUserRepo.Setup(x => x.GetByEmailAsync(requestModel.Email))
              .ReturnsAsync(user);

            // passwordHasher mock setup
            _mockPasswordHasher.Setup(x => x.VerifyPassword(requestModel.Password, user.PasswordHash))
                .Returns(true);
            // Act
            var act = async()=>await _authService.LoginAsync(requestModel);


            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Account is not active. Please contact administrator.");
        }
    }
}
