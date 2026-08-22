using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using ElectroStock.Infrastructure;
using ElectroStock.Tests.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.IntegrationTests
{
    [Collection("TestContainers")]
    public class AuthServiceTests
    {
        private readonly TestContainersFixture _fixture;

        public AuthServiceTests(TestContainersFixture fixture)
        {
            _fixture = fixture;
            _fixture.CleanDatabaseAsync().GetAwaiter().GetResult();
            _fixture = fixture;
        }
        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponseModel()
        {
            // Arrange
            // services setup
            using var scope = _fixture.ServiceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var createUserData = new UserCreationData
            {
                FirstName = "Registered",
                LastName = "User",
                PhoneNumber = "01234567890",
                Role = UserRole.Cashier,

            };
            var password = "cashier@123";
            var email = "cashier@gmail.com";

            var user = new RegisteredUser(
                createUserData,
                email:email,
                passwordHash: hasher.HashPassword(password)
                );
            
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();


            var model = new AuthRequestModel
            {
                Email= email,
                Password= password
            };

            // Act
            var loginResponse = await authService.LoginAsync(model);

            // Assert
            loginResponse.UserId.Should().Be(user.Id);
            loginResponse.Email.Should().Be(user.Email);
            loginResponse.Token.Should().NotBeNullOrWhiteSpace();

        }
    }
}
