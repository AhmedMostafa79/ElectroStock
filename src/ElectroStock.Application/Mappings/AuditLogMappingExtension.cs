using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Models;

namespace ElectroStock.Application.Mappings
{
    public static class AuditLogMappingExtension
    {
        public static AuditLogDto ToDto(this AuditLog auditLog)
        {
            if (auditLog == null)
                return null;
            return new AuditLogDto
            {
                Id = auditLog.Id,
                PerformedBy = auditLog.PerformedBy,
                Details = auditLog.Details,
                ReferenceType = auditLog.ReferenceType,
                CreatedAt = auditLog.CreatedAt,
            };
        }
    }
}
