using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Enums
{
    public enum ProductStatus
    {
        Draft,
        Active,
        Reserved,
        SoldOut,
        Damaged,
        Discontinued
    }
}
