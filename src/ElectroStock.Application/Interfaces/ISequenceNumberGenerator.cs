namespace ElectroStock.Application.Interfaces
{
    public interface ISequenceNumberGenerator
    {
        public Task<string> GenerateInvoiceNumberAsync();
        public Task<string> GenerateReservationNumberAsync();
    }
}
