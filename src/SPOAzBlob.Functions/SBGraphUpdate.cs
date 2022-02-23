using System;
using System.Text.Json;
using Azure.Storage.Blobs;
using CommonUtils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Functions
{
    public class SBGraphUpdate
    {
        private readonly ILogger _logger;

        public SBGraphUpdate(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SBGraphUpdate>();
        }

        [Function("SBGraphUpdate")]
        public void Run([ServiceBusTrigger("graphupdates", Connection = "ServiceBusConnectionString")] string messageContents, FunctionContext context)
        {
            var trace = (DebugTracer)context.InstanceServices.GetService(typeof(DebugTracer));
            var blobServiceClient = (BlobServiceClient)context.InstanceServices.GetService(typeof(BlobServiceClient));

            var update = JsonSerializer.Deserialize<GraphNotification>(messageContents);
            if (update != null && update.IsValid)
            {
                trace.TrackTrace($"Got Graph {update.Notifications.Count} updates from Service-Bus.");
            }
            else
            {
                trace.TrackTrace($"Got invalid Graph update notification with body '{messageContents}'");
            }

        }
    }
}
