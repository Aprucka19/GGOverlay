// Data/Counter.cs
using System;
using System.Threading.Tasks;

namespace GGOverlay.Data
{
    public class Counter
    {
        public int Value { get; private set; }
        public event Func<int, Task> OnValueChanged; // Event to notify when the counter value changes

        public Counter(int initialValue = 0)
        {
            Value = initialValue;
        }

        public void Increment()
        {
            SetValue(Value + 1);
        }

        public void Decrement()
        {
            SetValue(Value - 1);
        }

        public void SetValue(int newValue)
        {
            if (Value != newValue)
            {
                Value = newValue;
                OnValueChanged?.Invoke(Value); // Notify listeners when the value changes
            }
        }
    }
}
