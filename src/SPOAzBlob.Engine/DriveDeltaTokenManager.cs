using CommonUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine
{
    public class DriveDeltaTokenManager : AbstractGraphManager
    {
        private readonly AzureStorageManager _azureStorageManager;
        private string propNameForSite;
        public DriveDeltaTokenManager(Config config, DebugTracer trace, AzureStorageManager azureStorageManager) : base(config, trace)
        {
            propNameForSite = $"Delta:{_config.SharePointSiteId}";
            this._azureStorageManager = azureStorageManager;
        }

        public async Task SetToken(string token)
        {
            await _azureStorageManager.SetPropertyValue(propNameForSite, token);
        }

        public async Task<string?> GetToken()
        {
            var propVal = await _azureStorageManager.GetPropertyValue(propNameForSite);
            return propVal?.Value;
        }


        public async Task DeleteToken()
        {
            await _azureStorageManager.ClearPropertyValue(propNameForSite);
        }
    }
}
