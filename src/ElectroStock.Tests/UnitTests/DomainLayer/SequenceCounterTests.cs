using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class SequenceCounterTests
    {
        private static readonly DateOnly FixedDate = new(2025, 1, 1);
        [Fact]
        public void Constructor_WithValidData_CreatesSequenceCounter()
        {
            // Arrange
            var type = CounterType.Invoice;
            var startDate = FixedDate;

            // Act
            var counter = new SequenceCounter( type,startDate);

            // Assert
            counter.Id.Should().Be(1);
            counter.CounterType.Should().Be(type);  
            counter.CurrentDate.Should().Be(startDate);
            counter.LastNumber.Should().Be(0);
        }
        private SequenceCounter CreateTestCounter()
        {
            return new SequenceCounter(
                counterType:CounterType.Invoice,
                startDate: FixedDate
                );
        }
        [Fact]
        public void GetNextNumber_DateEqualCurrentDate_ReturnsNextNumber()
        {
            // Arrange
            var counter = CreateTestCounter();//sets CurrentDate=today
            var date = FixedDate;
            counter.GetNextNumber(date);// increments LastNumber by 1

            // Act
            var nextNumber = counter.GetNextNumber(date);

            // Assert
            nextNumber.Should().Be(2);
            counter.LastNumber.Should().Be(2);
        }

        [Fact]
        public void GetNextNumber_MultipleDays_ResetsCorrectly()
        {
            // Arrange
            var counter = CreateTestCounter();
            var day1 = FixedDate;

            // Act & Assert day 1
            counter.GetNextNumber(day1).Should().Be(1);// increments LastNumber by 1 = 1
            counter.LastNumber.Should().Be(1);
            counter.GetNextNumber(day1).Should().Be(2);// increments LastNumber by 1 = 2 

            // Act & Assert day 2
            var day2 = day1.AddDays(1);

            counter.GetNextNumber(day2).Should().Be(1);//resets to 0, then increments 1 = 1
            counter.LastNumber.Should().Be(1);
            counter.GetNextNumber(day2).Should().Be(2);// increments LastNumber by 1 = 2
            counter.CurrentDate.Should().Be(day2);    
           
            
            // Act & Assert day 3
            var day3= day2.AddDays(1);

            counter.GetNextNumber(day3).Should().Be(1);//resets to 0, then increments 1 = 1
            counter.LastNumber.Should().Be(1);
            counter.GetNextNumber(day3).Should().Be(2);// increments LastNumber by 1 = 2 
        } 
        
        [Fact]
        public void GetNextNumber_DateSmallerThanCurrentDate_ThrowsException()
        {
            // Arrange
            var counter = CreateTestCounter();
            var date = FixedDate;
            var dateToTest = date.AddDays(-1);
            // Act
            var act = ()=>counter.GetNextNumber(dateToTest);//pass yesterday's data

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Invalid date: {dateToTest}, date can't be in the past");
        }
      
    }   
}
