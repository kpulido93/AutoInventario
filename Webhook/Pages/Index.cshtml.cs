using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Services;

namespace Pages
{
    public class IndexModel : PageModel
    {
        private readonly EventStore _eventStore;

        public IndexModel(EventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public List<string> Events { get; private set; } = new();

        public void OnGet()
        {
            Events = _eventStore.GetEvents().ToList();
        }
    }
}
