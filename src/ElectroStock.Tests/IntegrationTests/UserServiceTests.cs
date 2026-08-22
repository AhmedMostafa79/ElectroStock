using ElectroStock.Application.Dtos;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Infrastructure;
using ElectroStock.Tests.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectroStock.Tests.IntegrationTests
{
    [Collection("TestContainers")]
    public class UserServiceTests
    {
        private readonly TestContainersFixture _fixture;
        public UserServiceTests(TestContainersFixture fixture)
        {
            _fixture = fixture;
            _fixture.CleanDatabaseAsync().GetAwaiter().GetResult();
        }
        [Fact]
        public async Task CreateStaffUserAsync_CreatesStaffUser_LogsUserCreation()
        {
            using var scope = _fixture.ServiceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Arrange
            var createUserData = new RegisterUserDto
            {
                FirstName = "Staff",
                LastName = "User",
                PhoneNumber = "01234567890",
                Email = "staff@gmail.com",
                Password = "staff@123",
                Role = UserRole.Cashier
            };
            var createdBy = Guid.NewGuid();

            // Act
            var userDto = await userService.CreateStaffUserAsync(createUserData, createdBy);

            // Assert

            // verify user is added
            var createdUser = await dbContext.Users
                .OfType<RegisteredUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == createUserData.Email);

            userDto.Should().BeEquivalentTo(createdUser.ToDto());

            // verify password is hashed successfully 
            var passwordHash = hasher.HashPassword(createUserData.Password);
            hasher.VerifyPassword(createUserData.Password, passwordHash)
                .Should().Be(true);

            // verify auditLog is created

            var auditLog = await dbContext.AuditLogs
                         .FirstOrDefaultAsync(a => a.ReferenceType == AuditLogType.StaffUserCreation);

            auditLog.PerformedBy.Should().Be(createdBy);
            auditLog.Details.Should().Contain($"{createUserData.Email}");
        }

    }
}
