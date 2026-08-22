using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Enums
{
    public enum InvoiceStatus
    {
        Pending=0,
        Completed=1,
        Cancelled = 2 //Voided/refunded
    }
}
