using ElectroStock.Domain.Enums;

namespace ElectroStock.Domain.Models
{
    public class SequenceCounter
    {
        public int Id { get; private set; }
        public DateOnly CurrentDate { get; private set; }
        public int LastNumber { get; private set; }
        public CounterType CounterType { get; private set; }
        private SequenceCounter() { }

        public SequenceCounter(CounterType counterType, DateOnly startDate)
        {
            // There is one counter record per CounterType.
            // so no stale records
            Id = (int)counterType;
            CounterType=counterType;
            CurrentDate = startDate;
            LastNumber = 0;
        }
        public int GetNextNumber(DateOnly date)
        {
            if (date < CurrentDate)
                throw new InvalidOperationException($"Invalid date: {date}, date can't be in the past");
           
            if (date > CurrentDate)
            {
                CurrentDate = date;
                LastNumber = 0;
            }
            LastNumber++;
            return LastNumber;
        }
    }
}
