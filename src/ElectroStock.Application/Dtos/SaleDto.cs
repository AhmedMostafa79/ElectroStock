using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Application.Dtos
{
    public class SaleDto
    {
        public Guid ProductId { get; init;}
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}
