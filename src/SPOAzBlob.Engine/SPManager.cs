using Azure.Identity;
using CommonUtils;
using CommonUtils.Config;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine
{
    public class SPManager
    {
        private GraphServiceClient _client;
        private Config _config;
        private DebugTracer _trace;
        private SPCache _cache;

        public SPManager(Config config, DebugTracer trace)
        {
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.Secret, options);
            _client = new GraphServiceClient(clientSecretCredential, scopes);
            _config = config;
            _trace = trace;


            _cache = new SPCache(config.SharePointSiteId, _client);

        }

        public async Task UploadDoc(string fileTitle, Stream fs)
        {
            //var listInfo = await _cache.GetList(_config.TargetListName);
            await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle).Content.Request().PutAsync<DriveItem>(fs);
        }
    }
}
