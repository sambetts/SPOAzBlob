using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
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
    public class FileManagerTests : AbstractTest
    {
        const string FILE_CONTENTS = "En un lugar de la Mancha, de cuyo nombre no quiero acordarme, no ha mucho tiempo que vivía un hidalgo de los de lanza en astillero, adarga antigua, rocín flaco y galgo corredor";

        [TestMethod]
        public async Task StartFileEditInSpo()
        {
            var _blobServiceClient = new BlobServiceClient(_config!.ConnectionStrings.Storage);

            // Upload a fake file to blob
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);
            var fileRef = containerClient.GetBlobClient($"UnitTest-{DateTime.Now.Ticks}.txt");
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                await fileRef.UploadAsync(fs, true);
            }

            // Generate a new shared-access-signature
            var sasUri = _blobServiceClient.GenerateAccountSasUri(AccountSasPermissions.Read,
                DateTime.Now.AddDays(1),
                AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

            // Start edit with new file
            var fm = new FileOperationsManager(_config!, _tracer);

            var azFileUrl = fileRef.Uri.AbsoluteUri + sasUri.Query;
            var newItem = await fm.StartFileEditInSpo(azFileUrl, _config.AzureAdAppDisplayName);
            Assert.IsNotNull(newItem);

            // Try and start edit the same file again. Should fail. 
            await Assert.ThrowsExceptionAsync<SpoFileAlreadyBeingEditedException>(async () => await fm.StartFileEditInSpo(azFileUrl, "Unit test user"));

        }

        // Upload a file to Az blob; start editing; 
        [TestMethod]
        public async Task UpdateAzFromSpo()
        {
            var azureStorageManager = new AzureStorageManager(_config!, _tracer);
            var _blobServiceClient = new BlobServiceClient(_config!.ConnectionStrings.Storage);


            // Clear locks
            var drive = await _client!.Sites[_config.SharePointSiteId].Drive.Request().GetAsync();
            var allLocks = await azureStorageManager.GetLocks(drive.Id);
            foreach (var l in allLocks)
            {
                await azureStorageManager.ClearLock(l);
            }
            var allLocksPostClear = await azureStorageManager.GetLocks(drive.Id);
            Assert.IsTrue(allLocksPostClear.Count == 0);


            // Prep: create file in Az and start to edit it, so we have a DriveItem
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);
            var fileRef = containerClient.GetBlobClient($"UnitTest-{DateTime.Now.Ticks}.txt");
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS)))
            {
                await fileRef.UploadAsync(fs, true);
            }

            var sasUri = _blobServiceClient.GenerateAccountSasUri(AccountSasPermissions.Read, DateTime.Now.AddDays(1), AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

            // Start editing a fake file
            var fm = new FileOperationsManager(_config!, _tracer);
            var azFileUrl = fileRef.Uri.AbsoluteUri + sasUri.Query;
            var newItem = await fm.StartFileEditInSpo(azFileUrl, _config.AzureAdAppDisplayName);

            // Clear delta token
            var dm = new DriveDeltaTokenManager(_config, _tracer, azureStorageManager);
            await dm.DeleteToken();

            // Now update Azure from SPO. As no delta token, all items in drive will be read.
            // We should hit our file, but it hasn't changed so won't be updated
            var fileUpdatedBackToAzure = await fm.ProcessSpoUpdatesForCorrespondingLocks();
            Assert.IsTrue(fileUpdatedBackToAzure.Count == 0);

            // Update SPO file again 
            using (var fs = new MemoryStream(Encoding.UTF8.GetBytes(FILE_CONTENTS + "v2")))
            {
                var result = await _client.Sites[_config.SharePointSiteId].Drive.Items[newItem.Id].Content
                                .Request()
                                .PutAsync<DriveItem>(fs);
            }


            // Now with delta do again. Should've updated 1 record as it has changed
            fileUpdatedBackToAzure = await fm.ProcessSpoUpdatesForCorrespondingLocks();
            Assert.IsTrue(fileUpdatedBackToAzure.Count == 1);
        }
    }
}
