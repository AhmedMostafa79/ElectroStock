using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines persistence and query operations for audit trail entries.
    /// </summary>
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Adds an audit log entry, optionally attaching it to the current business transaction context.
        /// </summary>
        /// <param name="auditLog">Audit log entry to persist.</param>
        /// <param name="isBusinessContext">
        /// <c>true</c> when the caller wants the audit entry to participate in an existing business transaction;
        /// otherwise the log can be saved independently.
        /// </param>
        public Task AddAsync(AuditLog auditLog,bool isBusinessContext=true);

        /// <summary>
        /// Retrieves audit logs created by a specific user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>Audit entries associated with the user.</returns>
        public Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId);

        /// <summary>
        /// Retrieves audit logs filtered by action type.
        /// </summary>
        /// <param name="auditType">Audit action classification.</param>
        /// <returns>Audit entries for the requested action type.</returns>
        public Task<IEnumerable<AuditLog>>GetByActionAsync(AuditLogType auditType);

        /// <summary>
        /// Retrieves audit logs within the specified time interval.
        /// </summary>
        /// <param name="start">Inclusive range start.</param>
        /// <param name="end">Inclusive range end.</param>
        /// <returns>Audit entries in the requested date range.</returns>
        public Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime start,DateTime end);

        /// <summary>
        /// Retrieves all audit logs.
        /// </summary>
        /// <returns>Complete audit trail entries.</returns>
        public Task<IEnumerable<AuditLog>>GetAllAsync();

        /// <summary>
        /// Flushes pending audit log changes to the audit context.
        /// </summary>
        /// <returns>Number of persisted state entries.</returns>
        public Task<int> SaveChangesAsyncAuditContext();

    }
}
