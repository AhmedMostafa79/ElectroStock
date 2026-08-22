using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class RegisteredUserTests
    {
        private UserCreationData CreateTestCreationData()
        {
            return new UserCreationData {
                PhoneNumber="01234567890",
                FirstName="Test",
                LastName="User",
                Role=UserRole.Admin
                };
        }

        [Theory]
        [InlineData("a124@gmail.com", "HashedPassword")]
        [InlineData("a123@gmail.com", "H123123123P")]
        [InlineData("admin@gmail.com", "h___ASDAS_D_ASDxc,  QWQWEp")]
        [InlineData("admin@gmail.com", "h")]
        public void Constructor_WithValidEmail_CreatesUser(string email, string passwordHash)
        {
            // Arrange
            var data = CreateTestCreationData();

            // Act
            var user = new RegisteredUser(
                data: data,
                email: email,
                passwordHash: passwordHash
                );

            // Assert
            user.Email.Should().Be(email);
            user.PasswordHash.Should().Be(passwordHash);
            user.Status.Should().Be(UserStatus.Active);
            user.UserType.Should().Be(UserType.Registered);
        }

        [Theory]
        [InlineData("")]    
        [InlineData("         ")]   
        [InlineData("a")]   
        [InlineData("a@gmail")] 
        [InlineData("a.com")]   
        [InlineData("@gmail.com")]
        [InlineData("1@gmail.com")]
        [InlineData("g@gmail.")]
        public void Constructor_WithInvalidEmail_ThrowsException(string email)
        {
            // Arrange
            var data = CreateTestCreationData();

            // Act
            var act = () => new RegisteredUser(
                 data: data,
                 email: email,
                 passwordHash: "HashedPassword"
                 );

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void HasEmail_ShouldReturnTrue()
        {

            // Arrange
            var creationData = new UserCreationData
            {
                PhoneNumber = "01234567890",
                FirstName = "Any",
                LastName = "User",
                Role = UserRole.Customer
            };

            // Act
            var user = new RegisteredUser(creationData,"admin@gmail.com","HashedPassword");

            // Assert
            user.HasEmail().Should().Be(true);
        }

        [Fact]
        public void Deactivate_DeactivatesUser()
        {

            // Arrange
            var creationData = CreateTestCreationData();
            var email = "admin@gmail.com";
            var passwordHash = "passwordHash";
            var user = new RegisteredUser(creationData, email, passwordHash);

            // Act
            user.Deactivate();

            // Assert
            user.Status.Should().Be(UserStatus.InActive);
        }

        [Theory]
        [InlineData("")]
        [InlineData("      ")]
        [InlineData(null)]
        public void ChangePassword_WithInvalidHash_ThrowsException(string passwordHash)
        {

            // Arrange
            var creationData = CreateTestCreationData();
            var email = "admin@gmail.com";

            // Act
            var act=()=>new RegisteredUser(creationData, email, passwordHash);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Password cannot be empty!");


        }
    }
}
