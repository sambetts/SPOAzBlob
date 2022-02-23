using CommonUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPOAzBlob.Engine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    [TestClass]
    public class Tests
    {
        #region Plumbing
        const string FILE_CONTENTS = "En un lugar de la Mancha, de cuyo nombre no quiero acordarme, no ha mucho tiempo que vivía un hidalgo de los de lanza en astillero, adarga antigua, rocín flaco y galgo corredor";

        private TestConfig? _config;
        private DebugTracer _tracer = DebugTracer.ConsoleOnlyTracer();

        [TestInitialize]
        public void Init()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true);


            var config = builder.Build();
            _config = new TestConfig(config);
        }
        #endregion

        [TestMethod]
        public async Task SPOUploadTest()
        {
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                var fileName = $"{DateTime.Now.Ticks}.txt";
                await sp.UploadDoc(fileName, fs);
            }
        }


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
