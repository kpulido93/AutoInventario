using Newtonsoft.Json;
using System.Collections.Generic;

namespace Models
{
    public class WebhookEvent
    {
        public string ClientID { get; set; }
        public string Data { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }
    }
}
