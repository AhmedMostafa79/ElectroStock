using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using FluentAssertions;

namespace ElectroStock.Tests.UnitTests.DomainLayer
{
    public class AuditLogTests
    {
        [Fact]
        public void Constructor_WithValidData_ShouldCreateAuditLog()
        {
            // Arrange
            var performedBy = Guid.NewGuid();
            var auditType = AuditLogType.InvoiceCreation;
            var details = $"Invoice is created, performed by: {performedBy}";
            
            // Act
            var auditLog = new AuditLog(
                performedBy: performedBy,
                referenceType: auditType,
                details:details
                );

            // Assert
            auditLog.PerformedBy.Should().Be( performedBy );
            auditLog.ReferenceType.Should().Be(auditType);
            auditLog.Details.Should().Be(details);
        }
    }
}
