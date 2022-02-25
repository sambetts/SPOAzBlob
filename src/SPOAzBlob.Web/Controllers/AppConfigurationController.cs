using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CommonUtils;
using Microsoft.AspNetCore.Mvc;
using SPOAzBlob.Engine;
using SPOAzBlob.Web.Models;

namespace SPO.ColdStorage.Web.Controllers
{
    /// <summary>
    /// Handles React app requests for app configuration
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AppConfigurationController : ControllerBase
    {
        private readonly DebugTracer _tracer;
        private readonly Config _config;

        public AppConfigurationController(Config config, DebugTracer tracer)
        {
            _tracer = tracer;
            this._config = config;
        }


        // Generate app ServiceConfiguration + storage configuration + key to read blobs
        // GET: AppConfiguration/ServiceConfiguration
        [HttpGet("[action]")]
        public ActionResult<ServiceConfiguration> GetServiceConfiguration()
        {
            var client = new BlobServiceClient(_config.ConnectionStrings.Storage);

            // Generate a new shared-access-signature
            var sasUri = client.GenerateAccountSasUri(AccountSasPermissions.List | AccountSasPermissions.Read,
                DateTime.Now.AddDays(1),
                AccountSasResourceTypes.Container | AccountSasResourceTypes.Object);

            // Return for react app
            return new ServiceConfiguration 
            {
                StorageInfo = new StorageInfo
                {
                    AccountURI = client.Uri.ToString(),
                    SharedAccessToken = sasUri.Query,
                    ContainerName = _config.BlobContainerName
                }
            };
        }
    }
}
