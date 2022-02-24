using Azure.Storage.Blobs;
using CommonUtils;
using Microsoft.Graph;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// High-level server-side funcionality of solution
    /// </summary>
    public class FileOperationsManager : AbstractGraphManager
    {
        private HttpClient _httpClient;
        private AzureStorageManager _azureStorageManager;
        private SPManager _spManager;
        public FileOperationsManager(Config config, DebugTracer trace) : base(config, trace)
        {
            _httpClient = new HttpClient();
            _azureStorageManager = new AzureStorageManager(config, trace);
            _spManager = new SPManager(config, trace);
        }

        /// <summary>
        /// User wants to start editing a file. Copy to SPO and create lock
        /// </summary>
        public async Task<DriveItem> StartFileEditInSpo(string azFileUrlWithSAS, string userName)
        {
            // See if file already exists
            string fileTitle = _azureStorageManager.GetFileTitleFromFQDN(new Uri(azFileUrlWithSAS));

            DriveItem? existingSpoDriveItem = null;
            try
            {
                existingSpoDriveItem = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle)
                    .Request()
                    .GetAsync();
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // File not found. This is fine.
                }
                else
                {
                    // Something else.
                    throw;
                }
            }
            
            // Check if this file is already being edited
            if (existingSpoDriveItem != null)
            { 
                // Do we have a lock for this file?
                var existingItemLock = await _azureStorageManager.GetLock(existingSpoDriveItem);
                if (existingItemLock != null)
                {
                    throw new SpoFileAlreadyBeingEditedException();
                }
            }

            // Get file from az blob & upload to SPO
            DriveItem? newFile = null;
            using (var fs = await _httpClient.GetStreamAsync(azFileUrlWithSAS))
            {
                newFile = await _spManager.UploadDoc(fileTitle, fs);
            }

            // Create lock for new file
            await _azureStorageManager.SetLock(newFile, userName);
            return newFile;
        }

        public async Task UpdateAzureWithSpoFile(GraphNotification graphNotification)
        {
        }
    }
}
