using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Enums
{
    public enum AuditLogType
    {
        // StockMovement
            StockReceipt=0,
            StockSale=1,
            StockRefund=2,
        // Product
            ProductCreation=10,
            ProductPublish=12,
            ProductPriceUpdate=13,
            ProductUpdate=14,
        
        //Reservation 
            ReservationCreation=20,
            ReservationConfirmation=21,
            ReservationExpiration=22,
            ReservationCancellation=23,

        //Invoice
           InvoiceCreation=30,
           InvoiceUpdate=31,
           InvoiceCancelation=32,

        //User
            StaffUserCreation=40,
            UserDeletion=41,

    }
}
