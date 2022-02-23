using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SPOAzBlob.Functions.Models
{
    public class GraphNotification
    {
        [JsonPropertyName("value")]
        public ChangeNotification[] Notifications { get; set; }
    }

    public class ChangeNotification
    {
        [JsonPropertyName("subscriptionId")]
        public Guid SubscriptionId { get; set; }

        [JsonPropertyName("clientState")]
        public object ClientState { get; set; }

        [JsonPropertyName("resource")]
        public string Resource { get; set; }

        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }

        [JsonPropertyName("resourceData")]
        public object ResourceData { get; set; }

        [JsonPropertyName("subscriptionExpirationDateTime")]
        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; }
    }
}
