import { BlobItem } from "@azure/storage-blob";


export interface DriveItem {
    webUrl: string
}

export const startAzureFileEdit = async (token: string, url: string): Promise<DriveItem> => {

    var urlEncoded = encodeURIComponent(url);

    return fetch('EditActions/StartEdit?url=' + urlEncoded, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                // Edit & lock applied. Return driveitem
                var driveItem: DriveItem = await response.json();

                return Promise.resolve(driveItem);
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};

export interface FileLock
{
    fileUrl: string,
    lockedByUser: string
}
export const getActiveLocks = async (token: string): Promise<FileLock[]> => {

    return fetch('EditActions/GetActiveLocks', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + token,
        }
    }
    )
        .then(async response => {

            if (response.ok) {
                var driveItem: FileLock[] = await response.json();

                return Promise.resolve(driveItem);
            }
            else {
                const dataText: string = await response.text();
                return Promise.reject(dataText);
            }
        });
};



export interface BlobWithLock
{
    blob: BlobItem,
    lock: FileLock | null
}