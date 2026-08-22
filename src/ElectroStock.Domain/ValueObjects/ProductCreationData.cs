using ElectroStock.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace ElectroStock.Domain.ValueObjects
{
    public class ProductCreationData
    {
        public string Sku { get;  set; }
        public string Brand { get;  set; }
        public string Name { get;  set; }
        public ProductCategory Category { get;  set; }
        public string Model { get;  set; }
        public decimal UnitCost { get;  set; }
        public decimal UnitPrice { get;  set; }

    }
}
