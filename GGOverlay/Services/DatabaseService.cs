using GGOverlay.Models;
using System.Linq;

namespace GGOverlay.Services
{
    public class DatabaseService
    {
        private readonly CounterContext _context;

        public DatabaseService()
        {
            _context = new CounterContext();
            _context.Database.EnsureCreated();
        }

        public int GetCounterValue()
        {
            return _context.Counters.FirstOrDefault()?.Value ?? 0;
        }

        public void UpdateCounterValue(int newValue)
        {
            var counter = _context.Counters.FirstOrDefault();
            if (counter == null)
            {
                counter = new Counter { Value = newValue };
                _context.Counters.Add(counter);
            }
            else
            {
                counter.Value = newValue;
            }
            _context.SaveChanges();
        }
    }
}
