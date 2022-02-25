import { StorageInfo } from '../../ConfigReader'
import React from 'react';
import { startAzureFileEdit, BlobWithLock } from '../../ApiLoader'

interface Props {
    storageInfo: StorageInfo,
    blobAndLock: BlobWithLock,
    token: string,
    refreshAllLocks: Function
}


export const File: React.FC<Props> = (props) => {

    const [loading, setLoading] = React.useState<boolean>(false);
    const [selected, setSelected] = React.useState<boolean>(false);

    const getFileName = (fullName: string) => {
        const dirs = fullName.split("/");
        return dirs[dirs.length - 1];
    }

    const getUrl = (fileName: String): string => {
        return props.storageInfo.accountURI + props.storageInfo.containerName + "/" + fileName
            + props.storageInfo.sharedAccessToken;
    }

    const clickDoc = () => {
        setSelected(!selected)
    }

    const startEdit = () => {
        setLoading(true);
        startAzureFileEdit(props.token, getUrl(props.blobAndLock.blob.name))
            .then(() => {
                setLoading(false);
                props.refreshAllLocks();
            }).catch(error => {
                setLoading(false);
                alert(error);
            })
    }

    return <div key={props.blobAndLock.blob.name}>
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-file-earmark-text" viewBox="0 0 16 16">
            <path d="M5.5 7a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5zM5 9.5a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5zm0 2a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5z" />
            <path d="M9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V4.5L9.5 0zm0 1v2A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5z" />
        </svg>
        <button onClick={clickDoc} className="link-button">
            {getFileName(props.blobAndLock.blob.name)}
        </button>
        {selected &&
            <div className='container'>
                <div className='flex-item'>
                    <a href={getUrl(props.blobAndLock.blob.name)}>Download</a>
                </div>
                {loading ?
                    <div>Loading...</div>
                    :
                    <div className='flex-item'>
                        {props.blobAndLock.lock !== null ?
                            <a href={props.blobAndLock.lock.fileUrl}>Open in SharePoint</a>
                            :
                            <button onClick={startEdit} className="link-button">Lock and Edit</button>
                        }
                    </div>
                }
            </div>
        }
    </div>
}
