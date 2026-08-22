using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure
{
    public class AuditDbContext:DbContext
    {
        public DbSet<AuditLog> AuditLogs {  get; set; }

        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
        { }
        
    }
}
