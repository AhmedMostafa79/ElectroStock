using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Services
{
    public class SequenceNumberGenerator : ISequenceNumberGenerator
    {
        private readonly ISequenceCounterRepository _counterRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SequenceNumberGenerator(ISequenceCounterRepository counterRepo, IUnitOfWork unitOfWork)
        {
            _counterRepo = counterRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateInvoiceNumberAsync() => await GenerateAsync(CounterType.Invoice,"INV");

        public async Task<string> GenerateReservationNumberAsync() => await GenerateAsync(CounterType.Reservation,"RES");

        private async Task<string> GenerateAsync(CounterType type, string prefix)
        {
                var counter = await _counterRepo.GetOrCreateAsync(type);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var sequence = counter.GetNextNumber(today);

                //_counterRepo.Update(counter);
            await _unitOfWork.SaveChangesAsync();

            return $"{prefix}-{today:yyyyMMdd}-{sequence:D4}";//e.g. "INV-20260523-0000"
        }
        
    }
}
