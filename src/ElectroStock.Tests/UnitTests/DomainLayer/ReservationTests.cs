using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class ReservationTests
    {

        private Reservation CreateTestReservation() {

            var reservation = new Reservation(
               reservationNumber: "RES-20260523-0000",
               productId: Guid.NewGuid(),
               customerId: Guid.NewGuid(),
               quantity: 5,
               unitPrice: 10,
               source: RequestSource.InStore
               );

            return reservation;
        }
        [Fact]
        public void Constructor_WithValidData_CreatesReservation()
        {
            // Arrange
            var reservationNumber = "RES-20260523-0000";
            var productId=Guid.NewGuid();
            var customerId=Guid.NewGuid();
            var quantity = 5;
            var unitPrice = 10;
            var requestSource = RequestSource.InStore;

            // Act
            var reservation = new Reservation(
                reservationNumber: reservationNumber,
                productId: productId,
                customerId: customerId,
                quantity: quantity,
                unitPrice: unitPrice,
                source: requestSource
                );

            // Assert
            reservation.ReservationNumber.Should().Be( reservationNumber );
            reservation.ProductId.Should().Be( productId );
            reservation.CustomerId.Should().Be( customerId );
            reservation.Quantity.Should().Be( quantity );
            reservation.UnitPrice.Should().Be( unitPrice );
            reservation.Source.Should().Be( requestSource );
            reservation.TotalPrice().Should().Be(quantity * unitPrice);
            reservation.ExpirationInMinutes.Should().Be(120);// 2 if in-store
        }
        
        [Theory]
        [InlineData(0,5)]
        [InlineData(-5,5)]
        [InlineData(5, 0)]
        [InlineData(5, -5)]
        public void Constructor_WithInvalidData_ThrowsException(int quantity,decimal unitPrice)
        {
            // Arrange
            var reservationNumber = "RES-20260523-0000";
            var productId=Guid.NewGuid();
            var customerId=Guid.NewGuid();
            var requestSource = RequestSource.InStore;

            // Act
            var act=()=> new Reservation(
                reservationNumber: reservationNumber,
                productId: productId,
                customerId: customerId,
                quantity: quantity,
                unitPrice: unitPrice,
                source: requestSource
                );

            // Assert
            act.Should().Throw<Exception>();
        }
        [Fact]
        public void Confirm_WhenActiveReservation_ConfirmsReservation()
        {

            // Arrange
            var reservation = CreateTestReservation();

            // Act
            reservation.Confirm();

            // Assert

            reservation.Status.Should().Be(ReservationStatus.Confirmed);
        }

        [Fact]
        public void Confirm_WhenInactiveReservation_ThrowsException()
        {

            // Arrange
            var reservation = CreateTestReservation();
            reservation.Cancel();

            // Act
            var act= ()=>reservation.Confirm();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Can't confirm reservation, current status: {ReservationStatus.Cancelled}");
        }
        [Fact]
        public void ExtendExpiryDate_WhenActiveReservation_ExtendsExpiration()
        {

            // Arrange
            var reservation = CreateTestReservation();
            var expirationBefore = reservation.ExpirationInMinutes;
            var expiresAt = reservation.ExpiresAt;

            var minutesToExtend = 120;
            // Act
            reservation.ExtendExpiryDate(minutesToExtend);

            // Assert
            reservation.ExpirationInMinutes.Should().Be(minutesToExtend + expirationBefore);// tests expirationInHours
            reservation.ExpiresAt.Should().Be(reservation.CreatedAt.AddMinutes(minutesToExtend + expirationBefore));//tests ExpiresAt logic
            reservation.ExpiresAt.Should().Be(expiresAt.AddMinutes(minutesToExtend));
        }

    }
}
