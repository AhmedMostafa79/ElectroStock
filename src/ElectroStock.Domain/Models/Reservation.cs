using ElectroStock.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Models
{
    public class Reservation
    {
        [Key]
        public Guid Id { get; private set; }

        [Required]
        [MaxLength(50)]
        public string ReservationNumber { get; private set; }

        [Required]
        public Guid ProductId { get; private set; }

        [Required]
        public Guid CustomerId { get; private set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; private set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; private set; }

        [Required]
        public RequestSource Source { get; private set; }

        [Required]
        public ReservationStatus Status { get; private set; }

        [Required]
        public DateTime CreatedAt { get; private set; }

        [Required]
        public int ExpirationInMinutes{ get; private set; }

        public DateTime ExpiresAt => CreatedAt.AddMinutes(ExpirationInMinutes);
        
        // Parameterless constructor for EF Core
       private Reservation() { }
        
        public Reservation(string reservationNumber, Guid productId, Guid customerId, 
            int quantity, decimal unitPrice, RequestSource source)
        {
            Id = Guid.NewGuid();
            ReservationNumber = reservationNumber; 
            ProductId = productId;
            CustomerId = customerId;

            if (quantity <= 0)
                throw new ArgumentException($"Quantity must be positive. Received: {quantity}");
            Quantity = quantity;

            if (unitPrice <= 0)
                throw new ArgumentException($"Unit price must be positive. Received: {UnitPrice}");

            UnitPrice = unitPrice;
            
            Source = source;
            Status = ReservationStatus.Pending;
            
            CreatedAt= DateTime.UtcNow;
            SetExpirationTime(source);
        }
        public void Confirm()
        {
            if (!IsActive())
                throw new InvalidOperationException($"Can't confirm reservation, current status: {Status}");

            Status = ReservationStatus.Confirmed;
        }
        public void Expire()
        {
            if (Status != ReservationStatus.Pending)
        throw new InvalidOperationException(
            $"Can't expire reservation, current status: {Status}");
            Status = ReservationStatus.Expired;
        }
        public void Cancel()
        {
            if(!IsActive())
                throw new InvalidOperationException($"Can't release reservation, current status: {Status}");

            Status = ReservationStatus.Cancelled;
        }
        public void ExtendExpiryDate(int minutes)
        {
            if(minutes <= 0)
                throw new InvalidOperationException($"Invalid number of hours {minutes}, can't extend expiration");

            if (!IsActive())
                throw new InvalidOperationException($"Can't extend expiration for reservation, current status: {Status}");

            ExpirationInMinutes += minutes;
        }
        private bool IsActive()
        {
           return  DateTime.UtcNow<=ExpiresAt &&
             Status == ReservationStatus.Pending;
        } 
        public decimal TotalPrice() => Quantity * UnitPrice;
        private void SetExpirationTime(RequestSource source)
        {
            ExpirationInMinutes = source switch
            {
                RequestSource.Phone => 60,
                RequestSource.InStore => 120,
                _=>15
            };
        }
    }
}
