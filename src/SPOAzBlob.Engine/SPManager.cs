using CommonUtils;
using Microsoft.Graph;
using SPOAzBlob.Engine.Models;

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
            if (cachedToken != null)
            {
                deltaCode = cachedToken.Code;
                _trace.TrackTrace($"Loading drive delta with code from {cachedToken.Timestamp}");
            }
            else
            {
                _trace.TrackTrace("Loading drive contents (all)");
            }

            var startingRequest = _client.Sites[_config.SharePointSiteId].Drive.Root.Delta(deltaCode)
                .Request()
                .Expand("LastModifiedByUser");
            return await GetDriveDeltaRecursive(startingRequest, true, dm);
        }

        private async Task<List<DriveItem>> GetDriveDeltaRecursive(IDriveItemDeltaRequest driveItemDeltaRequest, bool saveDelta, DriveDeltaTokenManager dm)
        {
            var deltaItems = await driveItemDeltaRequest.GetAsync();
            var deltaCodeUrl = deltaItems.AdditionalData["@odata.deltaLink"]?.ToString();

            if (saveDelta && !string.IsNullOrEmpty(deltaCodeUrl))
            {
                var code = DriveDelta.ExtractCodeFromGraphUrl(deltaCodeUrl);
                if (code != null)
                {
                    _trace.TrackTrace("Saving delta token for next contents query");

                    // Save delta token for next time
                    await dm.SetToken(code);
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
