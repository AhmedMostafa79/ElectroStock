using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;
using ElectroStock.Domain.Interfaces.Repositories;

namespace ElectroStock.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _businessContext;
        private readonly AuditDbContext _auditContext;
        public AuditLogRepository(AppDbContext dbContext,AuditDbContext auditContext)
        {
            _businessContext = dbContext;
            _auditContext = auditContext;
        }

        public async Task AddAsync(AuditLog auditLog, bool isBusinessContext = true)
        {
            if (isBusinessContext)
            {
                await _businessContext.AuditLogs.AddAsync (auditLog);
            }
            else
            {
                await _auditContext.AuditLogs.AddAsync(auditLog);

            }
        }
        public async Task AuditContextSaveChangesAsync()
        {
            await _auditContext.SaveChangesAsync();
        }
        public async Task<IEnumerable<AuditLog>> GetByActionAsync(AuditLogType auditType)
        {
            return await _businessContext.AuditLogs
                .Where(a => a.ReferenceType == auditType)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId)
        {
            return await _businessContext.AuditLogs
                .Where(a=>a.PerformedBy == userId)
                .AsNoTracking()
                .ToListAsync();
        }
        //public async Task<IEnumerable<AuditLog>> GetByEntityAsync(EntityType type)
        //{
        //    return await _dbContext.AuditLogs
        //        .Where()
        //}
        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _businessContext.AuditLogs
                .Where(a => start<= a.CreatedAt && a.CreatedAt <= end)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return await _businessContext.AuditLogs
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<int> SaveChangesAsyncAuditContext()
        {
            return await _auditContext.SaveChangesAsync();
        }


    }
}
