using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Mappings;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;

namespace ElectroStock.Application.Services
{

    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepo;
        public AuditLogService(IAuditLogRepository auditLogRepo)
        {
            _auditLogRepo=auditLogRepo;
        }

        // Persists the audit entry inside the caller transaction when requested; otherwise it saves immediately.
        public async Task LogAuditAsync(Guid performedBy, AuditLogType referenceType, string details,bool isInTransaction)
        {
            await _auditLogRepo.AddAsync(
                new AuditLog(
                    performedBy:performedBy,
                    referenceType: referenceType,
                    details:details
                ),
                isInTransaction);

            if (!isInTransaction)
                await _auditLogRepo.SaveChangesAsyncAuditContext();
        }
        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionAsync(AuditLogType auditType)
        {
            try
            {
                var auditLogs = await _auditLogRepo.GetByActionAsync(auditType);
                return auditLogs.Select(auditLog => auditLog.ToDto());

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex) {
                throw new Exception($"Failed to retrieve auditlogs with action type: {auditType}",ex);
            
            }
        }

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(Guid userId)
        {
            try
            {
           var auditLogs=await _auditLogRepo.GetByUserAsync(userId);
            return auditLogs.Select(auditLog => auditLog.ToDto());

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve auditlogs for user with id: {userId}",ex);

            }
        }

        //public Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(EntityType entityType)
        //{
        //    var auditLogs = await _auditLogRepo.By(userId);
        //    return auditLogs.Select(auditLog => auditLog.ToDto());
        //}

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime start, DateTime end)
        {
            try
            {
            var auditLogs = await _auditLogRepo.GetByDateRangeAsync(start, end);
            return auditLogs.Select(auditLog => auditLog.ToDto());

            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve auditlogs by data range start: {start} end: {end}", ex);

            }
        }

       
    }
}
