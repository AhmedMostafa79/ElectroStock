using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
   
    public class InvoiceItem
    {

        [Required]
        public Guid ProductId { get; private set; }
    
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; private set; }
    
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; private set; }

        // for EF Core
        private InvoiceItem() { }

        public InvoiceItem(Guid productId, int quantity, decimal unitPrice)
        {
            ProductId = productId;

            if (quantity <= 0)
                throw new ArgumentException($"Quantity must be positive. Received: {quantity}");
            Quantity = quantity;
            
                if (unitPrice <= 0)
                throw new ArgumentException($"Unit price must be positive. Received: {unitPrice}");
            UnitPrice = unitPrice;
        }
        public decimal TotalPrice()
        {
            return UnitPrice * Quantity;
        }
        
    }
}
