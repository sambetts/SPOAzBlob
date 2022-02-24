using CommonUtils;
using Microsoft.Graph;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Handle SharePoint Online specific operations
    /// </summary>
    public class SPManager : AbstractGraphManager
    {
        public SPManager(Config config, DebugTracer trace) :base (config, trace)
        {
        }

        public async Task<DriveItem> UploadDoc(string fileTitle, Stream fs)
        {
            var result = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle).Content
                .Request()
                .PutAsync<DriveItem>(fs);

            return result;
        }


        public async Task<List<DriveItem>> GetDriveItems(AzureStorageManager azureStorageManager)
        {
            var dm = new DriveDeltaTokenManager(_config, _trace, azureStorageManager);
            var deltaCode = string.Empty;

            // Cached delta code?
            var cachedToken = await dm.GetToken();
            if (!string.IsNullOrEmpty(cachedToken))
            {
                deltaCode = cachedToken;
            }

            var startingRequest = _client.Sites[_config.SharePointSiteId].Drive.Root.Delta(deltaCode)
                .Request()
                .Expand("LastModifiedByUser");
            return await GetDriveDeltaRecursive(startingRequest, true, dm);
        }

        private async Task<List<DriveItem>> GetDriveDeltaRecursive(IDriveItemDeltaRequest driveItemDeltaRequest, bool saveDelta, DriveDeltaTokenManager dm)
        {
            const string TOKEN_PARAM_NAME = "token";
            var deltaItems = await driveItemDeltaRequest.GetAsync();
            var deltaCodeUrl = deltaItems.AdditionalData["@odata.deltaLink"]?.ToString();

            if (saveDelta && !string.IsNullOrEmpty(deltaCodeUrl))
            {
                var uri = new Uri(deltaCodeUrl);
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);
                if (queryDictionary.AllKeys.Where(k => k == TOKEN_PARAM_NAME).Any())
                {
                    string deltaCode = queryDictionary[TOKEN_PARAM_NAME] ?? string.Empty;

                    // Save delta token for next time
                    await dm.SetToken(deltaCode);
                }
            }

            var returnItems = deltaItems.ToList();
            if (deltaItems.NextPageRequest != null)
            {
                var nextPageItem = await GetDriveDeltaRecursive(deltaItems.NextPageRequest, false, dm);
                returnItems.AddRange(nextPageItem);
            }

            return returnItems;
        }
    }
}
