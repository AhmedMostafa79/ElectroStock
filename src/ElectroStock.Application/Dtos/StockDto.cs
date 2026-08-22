namespace ElectroStock.Application.Dtos
{
    public class StockDto
    {
        public string Sku { get; init; }
        public int QuantityOnHand { get; init; }
        public int QuantityReserved {get;init;}
        public int QuantityAvailable { get;init;}
    }
}
