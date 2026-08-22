using ElectroStock.Domain.Enums;
using ElectroStock.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
    public class QuantifiedProduct : Product
    {
        // Parameterless constructor for EF Core materialization
        protected QuantifiedProduct() { }
        
        [Required]
        public int MinimumStockLevel { get; private set; }

        [Required]
        public int QuantityOnHand { get; private set; }

        [Required]
        public int QuantityReserved { get; private set; }
        public int AvailableQuantity => QuantityOnHand - QuantityReserved;
        public bool IsOutOfStock => QuantityOnHand <= 0;
        public QuantifiedProduct(ProductCreationData productData,int initialQuantity,
           int minimumStockLevel) : base(productData,ProductType.Quantified)
        {

            ValidateQuantity(minimumStockLevel);
            MinimumStockLevel=minimumStockLevel;

            ValidateQuantity(initialQuantity);
            QuantityOnHand =initialQuantity;
            
            QuantityReserved = 0;
        }

        private void ValidateQuantity(int quantity)
        {
            if (quantity <0)
                throw new ArgumentException($"Quantity can't be negative. Received: {quantity}");
        }
        public override bool CanReserveOrSell(int quantity)
        {
            ValidateQuantity(quantity);
            if (base.Status != ProductStatus.Active)
                return false;

            return quantity <= AvailableQuantity;
        }
        public override void Reserve(int quantity)
        {
            ValidateQuantity(quantity);

            if (!CanReserveOrSell(quantity))
                throw new InvalidOperationException("Product is not available or quantity is not sufficient");

            QuantityReserved += quantity;
        }
        public override void ReleaseReservation(int quantity)
        {
            ValidateQuantity(quantity);

            if (quantity > QuantityReserved)
                    throw new InvalidOperationException($"Cannot release {quantity}. Only {QuantityReserved} reserved");
            
            QuantityReserved -= quantity;
        }
        public override void Refund(int quantity = 1)
        {
            ReceiveQuantity(quantity);
        }
        public override void ConfirmSale(int quantity)
        {
            ValidateQuantity(quantity);
         
            if (!CanReserveOrSell(quantity))
                throw new InvalidOperationException("Product is not available or quantity is not sufficient");

            QuantityOnHand -= quantity;

        }

        public override void ConfirmReservedSale(int quantity)
        {
            ValidateQuantity(quantity);

            if (Status!=ProductStatus.Active)// didn't use CanReserveOrSell, as they already reserved
                throw new InvalidOperationException("Product is not available or quantity is not sufficient");


            if (quantity> QuantityReserved)
                throw new InvalidOperationException($"Cannot confirm sale for {quantity}. Only {QuantityReserved} reserved");

            QuantityReserved -= quantity;
            QuantityOnHand -= quantity;

        }

        public void ReceiveQuantity(int quantity)
        {
            ValidateQuantity(quantity);

            if (Status == ProductStatus.Discontinued)
                throw new InvalidOperationException($"Cannot receive quantity for product with status {ProductStatus.Discontinued}");

            QuantityOnHand += quantity;

        }

       
    }
}
