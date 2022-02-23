using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using CommonUtils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Functions
{
    public static class GraphNotifications
    {
        /// <summary>
        /// Endpoint for Graph to call us with change notifications. 
        /// Must reply within 3 seconds or we'll get delayed responses - https://docs.microsoft.com/en-us/graph/webhooks#change-notifications
        /// </summary>
        [Function("GraphNotifications")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            var trace = (DebugTracer)context.InstanceServices.GetService(typeof(DebugTracer));

            var response = req.CreateResponse(HttpStatusCode.OK);

            // Is this a Graph validation call?
            const string VALIDATION_PARAM_NAME = "validationToken";
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            if (queryDictionary.AllKeys.Where(k=> k == VALIDATION_PARAM_NAME).Any())
            {
                trace.TrackTrace("Got Graph API validation call .Returning validation token.");
                response.WriteString(queryDictionary[VALIDATION_PARAM_NAME]);

                return response;
            }

            var sbSender = (ServiceBusSender)context.InstanceServices.GetService(typeof(ServiceBusSender));


            // Figure out update
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var update = JsonSerializer.Deserialize<GraphNotification>(requestBody);
            if (update != null && update.IsValid)
            {
                var sbMsg = new ServiceBusMessage(requestBody);
                await sbSender.SendMessageAsync(sbMsg);
                trace.TrackTrace($"Got Graph {update.Notifications.Count} updates from Graph. Sent to Service-Bus queue for async processing.");
            }
            else
            {
                trace.TrackTrace($"Got invalid Graph update notification with body '{requestBody}'");
            }

            return response;
        }
    }
}
