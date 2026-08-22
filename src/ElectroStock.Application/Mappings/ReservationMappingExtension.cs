using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Models;

namespace ElectroStock.Application.Mappings
{
    public static class ReservationMappingExtension
    {
        public static ReservationDto ToDto(this Reservation reservation)
        {
            if (reservation == null)
                return null;

        //      public Guid Id { get; init; }

        //public string ReservationNumber { get; init; }

        //public Guid ProductId { get; init; }

        //public Guid CustomerId { get; init; }

        //public int Quantity { get; init; }

        //public decimal UnitPrice { get; init; }
        //public decimal TotalPrice { get; init; }

        //public ReservationSource Source { get; init; }

        //public ReservationStatus Status { get; init; }

        //public DateTime CreatedAt { get; init; }

        //public DateTime ExpiresAt { get; init; }
            return new ReservationDto
            {
               Id=reservation.Id,
               ReservationNumber=reservation.ReservationNumber,
               ProductId=reservation.ProductId,
               CustomerId=reservation.CustomerId,
               Quantity=reservation.Quantity,
               UnitPrice=reservation.UnitPrice,
               TotalPrice=reservation.TotalPrice(),
              Source=reservation.Source,
              Status=reservation.Status,
              CreatedAt=reservation.CreatedAt,
              ExpiresAt=reservation.ExpiresAt
            };
        }
    }
}
