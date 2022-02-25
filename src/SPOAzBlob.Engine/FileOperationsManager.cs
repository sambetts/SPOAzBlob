﻿using CommonUtils;
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
            var azFileUri = new Uri(azFileUrlWithSAS);
            string fileTitle = _azureStorageManager.GetFileTitleFromFQDN(azFileUri);

            // See if file already exists
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
                else throw;   // Something else.
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
                newFile = await _spManager.UploadDoc(fileTitle, fs);

            // Create lock for new file
            await _azureStorageManager.SetOrUpdateLock(newFile, azFileUri.AbsoluteUri.Replace(azFileUri.Query, string.Empty), userName);
            return newFile;
        }

        /// <summary>
        /// Get recent updates (or all files if 1st time). For each lock, update Azure blob contents for corresponding SP file
        /// </summary>
        public async Task<List<DriveItem>> ProcessSpoUpdatesForActiveLocks()
        {
            // Figure out latest changes
            var spManager = new SPManager(_config, _trace);
            var spItemsChanged = await spManager.GetDriveItems(_azureStorageManager);

            var updatedItems = new List<DriveItem>();
            if (spItemsChanged.Count > 0)
            {

                // If something has changed in SPO, compare locks to change list
                var driveId = spItemsChanged[0].ParentReference.DriveId;
                var allCurrentLocks = await _azureStorageManager.GetLocks(driveId);

                _trace.TrackTrace($"{nameof(ProcessSpoUpdatesForActiveLocks)}: Found {spItemsChanged.Count} SharePoint updates and {allCurrentLocks.Count} active locks.");

                foreach (var currentLock in allCurrentLocks)
                {
                    var spoDriveItem = spItemsChanged.Where(i => i.Id == currentLock.RowKey).SingleOrDefault();
                    if (spoDriveItem != null)
                    {
                        var success = false;
                        try
                        {
                            success = await UpdateAzureFile(spoDriveItem, currentLock);
                        }
                        catch (UpdateConflictException ex)
                        {
                            _trace.TrackTrace("Couldn't update Azure file from updated SPO file.", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                            _trace.TrackExceptionAndLogToTrace(ex);
                        }

                        if (success)
                        {
                            updatedItems.Add(spoDriveItem);
                        }
                    }
                }

            }
            return updatedItems;
        }

        private async Task<bool> UpdateAzureFile(DriveItem spoDriveItem, FileLock currentLock)
        {
            var userName = GetUserName(spoDriveItem.LastModifiedBy);

            if (currentLock.FileContentETag != spoDriveItem.CTag)
            {
                // Update lock 1st
                currentLock.FileContentETag = spoDriveItem.CTag;
                await _azureStorageManager.SetOrUpdateLock(spoDriveItem, currentLock.AzureBlobUrl, userName);

                // Upload to SP
                await _azureStorageManager.UploadSharePointFileToAzureBlob(spoDriveItem, userName);

                return true;
            }
            return false;
        }

        private string GetUserName(IdentitySet identitySet)
        {
            if (identitySet is null)
            {
                throw new ArgumentNullException(nameof(identitySet));
            }

            if (identitySet.User != null)
            {
                // Maybe use UPN here
                return identitySet.User.DisplayName;
            }
            else if (identitySet.Application != null)
            {
                return identitySet.Application.DisplayName;
            }

            throw new ArgumentOutOfRangeException(nameof(identitySet));
        }
    }
}
