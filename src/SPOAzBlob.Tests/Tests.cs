using Azure.Storage.Blobs;
using CommonUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
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
            DriveItem? spoDoc = null;
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                var fileName = $"{DateTime.Now.Ticks}.txt";
                spoDoc = await sp.UploadDoc(fileName, fs);
            }

            Assert.IsNotNull(spoDoc);
        }

        [TestMethod]
        public async Task UploadSpoFileToAzureTests()
        {
            // Create new file & upload
            var fileName = $"{DateTime.Now.Ticks}.txt";
            DriveItem? spoDoc = null;
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                spoDoc = await sp.UploadDoc(fileName, fs);
            }

            var fileUpdater = new AzureStorageManager(_config!, _tracer);
            await fileUpdater.UploadSharePointFileToAzureBlob(fileName, "Unit tester");

            // Create a lock for another user for this file
            await fileUpdater.SetLock(spoDoc, "Bob");

            // Now when we upload again, we should get an error
            await Assert.ThrowsExceptionAsync<FileLockedByAnotherUserException>(async () => await fileUpdater.UploadSharePointFileToAzureBlob(fileName, "Unit tester"));

            // Update file in SPO but don't update lock. Try uploading again with old lock still in place.
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS + "v2")))
            {
                spoDoc = await sp.UploadDoc(fileName, fs);
            }
            await Assert.ThrowsExceptionAsync<FileUpdateConflictException>(async () => await fileUpdater.UploadSharePointFileToAzureBlob(fileName, "Bob"));

        }



        [TestMethod]
        public async Task FileLocksReadAndWriteTest()
        {
            DriveItem? spoDoc = null;
            var sp = new SPManager(_config!, _tracer);
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                var fileName = $"{DateTime.Now.Ticks}.txt";
                spoDoc = await sp.UploadDoc(fileName, fs);
            }

            var azManager = new AzureStorageManager(_config!, _tracer);

            // No lock for this new doc yet. Make sure it's null
            var fileLock = await azManager.GetLock(spoDoc);
            Assert.IsNull(fileLock);

            // Set new lock & check again
            await azManager.SetLock(spoDoc, "Unit tester");
            fileLock = await azManager.GetLock(spoDoc);
            Assert.IsNotNull(fileLock);


            // Set lock for same file but different user. Should fail
            await Assert.ThrowsExceptionAsync<FileLockedByAnotherUserException>(async () => await azManager.SetLock(spoDoc, "Unit tester2"));

            // Clear lock & check again
            await azManager.ClearLock(spoDoc);
            fileLock = await azManager.GetLock(spoDoc);
            Assert.IsNull(fileLock);
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
