using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPOAzBlob.Engine;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    [TestClass]
    public class GraphTests : AbstractTest
    {
        // Won't work without a public endpoint (TestGraphNotificationEndpoint) listening for notifications. 
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

        [TestMethod]
        public async Task UserManagerTests()
        {
            var userManager = new GraphUserManager(_config!, _tracer);
            var user = await userManager.GetUserByEmail(_config!.TestEmailAddress);

            Assert.IsNotNull(user);
        }
    }
}
