using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Enums
{
    public enum ProductCategory
    {
        // ===== Devices with IMEI (Cellular/Connected) =====
        Smartphone = 0,
        Tablet = 1,
        SmartWatch=2,

        // ===== Electronics without IMEI =====
        Laptop = 10,
        Audio=11,
        HeadPhone=12,
        // ===== Accessories =====

        Cable = 20,
        Charger=21,
        PowerBank=22,
        Case=23,
        Accessory=24
    }
}
