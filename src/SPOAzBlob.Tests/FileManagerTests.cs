using Azure.Storage.Blobs;
using Azure.Storage.Sas;
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
            var newItem = await fm.StartFileEditInSpo(azFileUrl, "Unit test user");
            Assert.IsNotNull(newItem);

            // Try and start edit the same file again. Should fail. 
            await Assert.ThrowsExceptionAsync<SpoFileAlreadyBeingEditedException>(async () => await fm.StartFileEditInSpo(azFileUrl, "Unit test user"));

        }

    }
}
