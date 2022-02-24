using System;
using System.Threading.Tasks;
using CommonUtils;
using Microsoft.Azure.Functions.Worker;
using SPOAzBlob.Engine;

namespace SPOAzBlob.Functions
{
    public class SubscriptionRefresh
    {
        // Every 2 mins: 0 */2 * * * *
        // Every day: 0 0 * * *
        [Function("SubscriptionRefresh")]
        public static async Task Run([TimerTrigger("0 0 * * *")] SubscriptionRefreshInfo timerInfo, FunctionContext context)
        {
            var trace = (DebugTracer)context.InstanceServices.GetService(typeof(DebugTracer));
            var config = (Config)context.InstanceServices.GetService(typeof(Config));

            trace.TrackTrace($"SubscriptionRefresh executed.");

#if DEBUG
            const string PROTOCOL = "http";
#else
            const string PROTOCOL = "https";
#endif
            // Autodetect webhook URL, or use override
            var url = $"{PROTOCOL}://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/GraphNotifications";
            if (!string.IsNullOrEmpty(config.WebhookUrlOverride))
            {
                url = config.WebhookUrlOverride;
            }

            // Renew/create webhooks
            var webhooksManager = new WebhooksManager(config, trace, url);
            try
            {
                await webhooksManager.CreateOrUpdateWebhook();
                trace.TrackTrace($"Webhooks created/renewed successfully");
            }
            catch (Exception ex)
            {
                trace.TrackTrace($"Webhooks failed to create/renew for URL '{url}'", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical);
                trace.TrackExceptionAndLogToTrace(ex);
            }

            trace.TrackTrace($"Next timer schedule at: {timerInfo.ScheduleStatus.Next}");
        }
    }

    public class SubscriptionRefreshInfo
    {
        public SubscriptionRefreshScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class SubscriptionRefreshScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
