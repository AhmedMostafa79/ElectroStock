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
    public class SerializedProduct : Product
    {
        // Parameterless constructor for EF Core materialization
        protected SerializedProduct() { }
        
        [Required]
        [MaxLength(100)]
        public string SerialNumber { get; private set; }

        [MaxLength(15)]
        public string? Imei { get; private set; }


        public DateTime? SoldDate { get;private set; }
        public WarrantyPeriod WarrantyPeriod { get; private set; }

        public SerializedProduct(ProductCreationData productData,
            string serialNumber, string? imei,
            WarrantyPeriod warrantyPeriod)   :base(productData, ProductType.Serialized) {
            
            ValidateSerialNumber(serialNumber);
            SerialNumber = serialNumber;

            ValidateImei(productData.Category, imei);
            Imei = imei;

            SoldDate = null;

            WarrantyPeriod= warrantyPeriod;
        }
       
        public override bool CanReserveOrSell(int quantity=1)
        {
            return quantity==1 && base.Status == ProductStatus.Active;
        }
        
        public override void ConfirmSale(int quantity=1)
        {
            if(!CanReserveOrSell(quantity))
                throw new InvalidOperationException($"Item is unavailable or insufficient quantity");

            base.ChangeStatus(ProductStatus.SoldOut);
            SoldDate=DateTime.UtcNow;
        }

        public override void ConfirmReservedSale(int quantity=1)
        {
            if (quantity != 1)
                throw new InvalidOperationException($"Insufficient quantity {quantity}");

            if (Status != ProductStatus.Reserved)
                throw new InvalidOperationException($"Product is not reserved! or is Sold out, product status {Status}");

            base.ChangeStatus(ProductStatus.SoldOut);
            SoldDate = DateTime.UtcNow;
        }

        public override void Reserve(int quantity=1)
        {
            if(Status==ProductStatus.Reserved)
                throw new InvalidOperationException("Item is already reserved");

            if (!CanReserveOrSell(quantity))
                throw new InvalidOperationException($"Cannot reserve item. Current status: {base.Status}");

            
            base.ChangeStatus(ProductStatus.Reserved);
        }
        public override void Refund(int quantity=1)
        {
            base.Publish();
        }
        public override void ReleaseReservation(int quantity=1)
        {
            if (quantity != 1)
                throw new InvalidOperationException($"Insufficient quantity {quantity}");

            if (base.Status != ProductStatus.Reserved)
                throw new InvalidOperationException($"Item is not reserved. Current status: {base.Status}");
            
            base.ChangeStatus(ProductStatus.Active);
        }
        public bool IsUnderWarranty()
        {
            if (!SoldDate.HasValue)
                return false;

            int warrantyMonths = (int)WarrantyPeriod;
            var warrantyExpiryDate= SoldDate.Value.AddMonths(warrantyMonths);
            return DateTime.UtcNow<= warrantyExpiryDate;
        }
        private void ValidateSerialNumber(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException("Serial number is required, can't be empty or null");

            if (serialNumber.Length < 5 || serialNumber.Length > 50)
                throw new ArgumentException("Serial number must be between 5 and 50 characters");
        }
        private bool RequiresImei(ProductCategory category)
        {
            // Categories 0-9 are phones/tablets that require IMEI
            return (int)category >= 0 && (int)category < 10;
        }
        private void ValidateImei(ProductCategory category, string? imei)
        {

            if (!RequiresImei(category))
                return;

            int imeiDigitsCnt = 15;

            if (string.IsNullOrWhiteSpace(imei) || imei.Length != imeiDigitsCnt ||
                    imei.Count(char.IsDigit) != imeiDigitsCnt)
                throw new ArgumentException($"IMEI must consist of exactly 15 digits {imei}");

            if(!IsValidLuhn(imei))
                throw new ArgumentException($"Invalid IMEI checksum {imei}");
        }

        private bool IsValidLuhn(string imei)
        {
            if (!imei.All(char.IsDigit))
                return false;

            int sum = 0;
            bool alternate = false;

            for (int i = imei.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(imei[i].ToString());

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit = (digit % 10) + 1;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

       
    }
}
