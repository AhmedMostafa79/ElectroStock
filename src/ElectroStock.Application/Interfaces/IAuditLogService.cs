using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Interfaces
{
    /// <summary>
    /// Provides audit reporting and persistence for system actions performed by users.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Records an audit entry for an action, optionally as part of an existing transaction.
        /// </summary>
        /// <param name="performedBy">The user who triggered the action.</param>
        /// <param name="referenceType">The audit event classification.</param>
        /// <param name="details">The human-readable event description.</param>
        /// <param name="isInTransaction">When true, the log is persisted within the current transaction.</param>
        public Task LogAuditAsync(Guid performedBy, AuditLogType referenceType, string details, bool isInTransaction);

        /// <summary>
        /// Retrieves all audit entries for a given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Audit log entries for that user.</returns>
        public Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(Guid userId);

        /// <summary>
        /// Retrieves audit entries filtered by action type.
        /// </summary>
        /// <param name="auditType">The audit action type.</param>
        /// <returns>Matching audit entries.</returns>
        public Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionAsync(AuditLogType auditType);

        /// <summary>
        /// Retrieves audit entries within a date range.
        /// </summary>
        /// <param name="start">The inclusive start date/time.</param>
        /// <param name="end">The inclusive end date/time.</param>
        /// <returns>Audit entries between the supplied dates.</returns>
        public Task<IEnumerable<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime start, DateTime end);
    }
}
