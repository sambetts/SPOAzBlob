using Azure.Identity;
using CommonUtils;
using Microsoft.Graph;

namespace SPOAzBlob.Engine
{
    public class SPManager : GraphManager
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

    }
}
