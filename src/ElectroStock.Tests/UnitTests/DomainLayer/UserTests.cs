using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class UserTests
    {
        [Theory]
        [InlineData("00000000000")]
        [InlineData("00000111112")]
        [InlineData("00000000000")]
        public void Constructor_WithValidData_CreatesCorrectly(string phoneNumber)
        {
           // Arrange
            var creationData = new UserCreationData {
                PhoneNumber= phoneNumber,
                FirstName="Any",
                LastName = "User",
                Role= UserRole.Customer
                };

            // Act
            var user = new GuestUser(data:creationData);

            // Assert
            user.Phone.Should().Be(creationData.PhoneNumber);
            user.FirstName.Should().Be(creationData.FirstName);
            user.LastName.Should().Be(creationData.LastName);
            user.Role.Should().Be(creationData.Role);
            user.UserType.Should().Be(UserType.Guest);
        }
        
        [Theory]
        [InlineData(null)]// null test
        [InlineData("")]// empty test
        [InlineData("0123456789")]//10 digits, while needed 11
        [InlineData("012345678900")]//12 digits, while needed 11
        [InlineData("012a4567890")]//10 digits, with 1 non digit
        public void Constructor_WithInvalidPhoneNumber_ThrowsException(string phoneNumber)
        {
           // Arrange
            var creationData = new UserCreationData {
                PhoneNumber=phoneNumber,
                FirstName="Any",
                LastName = "User",
                Role= UserRole.Customer,
                };

            // Act
            var act=()=> new GuestUser(data:creationData);

            // Assert
           act.Should().Throw<ArgumentException>();
        }
    }
}
