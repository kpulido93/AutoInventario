using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Services
{
    public class EventStore
    {
        private readonly ConcurrentQueue<string> _events = new();

        public void AddEvent(string eventData)
        {
            _events.Enqueue(eventData);

            // Limitar la cantidad de eventos almacenados (ejemplo: 100 eventos)
            if (_events.Count > 100)
            {
                _events.TryDequeue(out _);
            }
        }

        public IEnumerable<string> GetEvents() => _events.ToArray();
    }
}
