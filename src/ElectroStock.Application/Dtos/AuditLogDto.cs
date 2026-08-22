using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Dtos
{
    public class AuditLogDto
    {
        public Guid Id { get; init; }
        public Guid? PerformedBy { get; init; }

        public string Details { get; init; }

        public AuditLogType ReferenceType { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
