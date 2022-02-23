﻿using Azure;
using Azure.Data.Tables;
using Microsoft.Graph;

namespace SPOAzBlob.Engine.Models
{
    public class FileLock : ITableEntity
    {
        public FileLock()
        { 
        }

        public FileLock(DriveItem driveItem, string userName)
        {
            var encoded = System.Net.WebUtility.UrlEncode(driveItem.WebUrl);
            this.PartitionKey = encoded;
            this.RowKey = encoded;
            this.FileContentETag = driveItem.CTag;
            this.LockedByUser = userName;
        }

        public string LockedByUser { get; set; } = string.Empty;
        public string FileUrl => System.Net.WebUtility.UrlDecode(PartitionKey);
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = String.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string FileContentETag { get; set; } = string.Empty;
    }
}