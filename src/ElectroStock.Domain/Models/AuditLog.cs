using ElectroStock.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; private set; }
        public Guid? PerformedBy { get; private set; }

        [MaxLength(500)]
        public string Details { get; private set; }

        [Required]
        public AuditLogType ReferenceType { get; private set; }
        
        [Required]
        public DateTime CreatedAt { get; private set; }

        public AuditLog( Guid? performedBy, AuditLogType referenceType,string details="")
        {
            Id = Guid.NewGuid();
            PerformedBy = performedBy;
            ReferenceType=referenceType ;
            Details = details;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
