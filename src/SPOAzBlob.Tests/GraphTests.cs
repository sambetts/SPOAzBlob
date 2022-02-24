using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPOAzBlob.Engine;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    [TestClass]
    public class GraphTests : AbstractTest
    {

        [TestMethod]
        public async Task WebhooksManagerTests()
        {
            var webhooksManager = new WebhooksManager(_config!, _tracer, _config!.TestGraphNotificationEndpoint);
            await webhooksManager.DeleteWebhooks();
            var noWebHooksValidResult = await webhooksManager.HaveValidWebhook();

            Assert.IsFalse(noWebHooksValidResult);

            await webhooksManager.CreateOrUpdateWebhook();
            var webHooksCreatedValidResult = await webhooksManager.HaveValidWebhook();

            Assert.IsTrue(webHooksCreatedValidResult);
        }
    }
}
