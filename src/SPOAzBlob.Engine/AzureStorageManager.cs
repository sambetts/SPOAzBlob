using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage.Blobs;
using CommonUtils;
using Microsoft.Graph;
using SPOAzBlob.Engine.Models;
using System.IO;

namespace SPOAzBlob.Engine
{
    public class AzureStorageManager : AbstractGraphManager
    {
        private BlobServiceClient _blobServiceClient;
        private TableServiceClient _tableServiceClient;
        public AzureStorageManager(Config config, DebugTracer trace) : base(config, trace)
        {
            _tableServiceClient = new TableServiceClient(_config.ConnectionStrings.Storage);
            _blobServiceClient = new BlobServiceClient(_config.ConnectionStrings.Storage);
        }

        public async Task UploadDocToAzureBlob(string fileTitle, string userName)
        {

            // Get drive item
            var driveItem = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle).Request().GetAsync();

            var existingLock = await GetLock(driveItem);
            if (existingLock != null)
            {
                // Does someone else have another lock?
                if (existingLock.LockedByUser != userName)
                {
                    throw new FileLockedAlreadyException(existingLock.LockedByUser);
                }

                // Was the SP file updated before our lock?
                if (driveItem.LastModifiedDateTime <= existingLock.Timestamp && existingLock.FileContentETag != driveItem.CTag)
                {
                    throw new FileUpdateConflictException(existingLock.LockedByUser);
                }
            }

            // Copy to az blob
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.BlobContainerName);
            var fileRef = containerClient.GetBlobClient(fileTitle);
            using (var fs = await _client.Sites[_config.SharePointSiteId].Drive.Root.ItemWithPath(fileTitle).Content.Request().GetAsync())
            {
                await fileRef.UploadAsync(fs, true);
            }

            throw new NotImplementedException();
        }

        public async Task<FileLock?> GetLock(DriveItem driveItem)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);
            var queryResultsFilter = tableClient.QueryAsync<FileLock>(f => f.RowKey == System.Net.WebUtility.UrlEncode(driveItem.WebUrl));

            // Iterate the <see cref="Pageable"> to access all queried entities.
            await foreach (var qEntity in queryResultsFilter)
            {
                return qEntity;
            }

            // No results
            return null;
        }
        public async Task SetLock(DriveItem driveItem, string userName)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);

            var entity = new FileLock(driveItem, userName);

            // Entity doesn't exist in table, so invoking UpsertEntity will simply insert the entity.
            await tableClient.UpsertEntityAsync(entity);
        }


        public async Task ClearLock(DriveItem driveItem)
        {
            var tableClient = await GetTableClient(_config.AzureTableLocks);
            var encoded = System.Net.WebUtility.UrlEncode(driveItem.WebUrl);
            tableClient.DeleteEntity(encoded, encoded);
        }

        async Task<TableClient> GetTableClient(string tableName)
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(_config.AzureTableLocks);
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            return tableClient;
        }
    }
}
