using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class GuestUserTests
    {
        [Fact]
        public void UserType_IsGuest()
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
            var user= new GuestUser(creationData);

            // Assert
            user.UserType.Should().Be(UserType.Guest);
        }

        [Fact]
        public void HasEmail_ShouldReturnFalse()
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
            var user= new GuestUser(creationData);

            // Assert
            user.HasEmail().Should().Be(false);
        }
    }
}
