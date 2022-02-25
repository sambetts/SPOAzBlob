using CommonUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using SPOAzBlob.Engine;
using SPOAzBlob.Engine.Models;

namespace SPO.ColdStorage.Web.Controllers
{
    /// <summary>
    /// Handles React app requests for editing files
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EditActionsController : ControllerBase
    {
        private readonly DebugTracer _tracer;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly Config _config;

        public EditActionsController(Config config, DebugTracer tracer, GraphServiceClient graphServiceClient)
        {
            _tracer = tracer;
            this._graphServiceClient = graphServiceClient;
            this._config = config;
        }


        // Start editing a document in SPO
        // POST: EditActions/StartEdit?url=1232
        [HttpPost("[action]")]
        public async Task<ActionResult<DriveItem>> StartEdit(string url)
        {
            var emailClaimValue = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
            var um = new GraphUserManager(_config, _tracer);
            if (string.IsNullOrEmpty(emailClaimValue))
            {
                return BadRequest("No email claim in user");
            }
            var user = await um.GetUserByEmail(emailClaimValue);

            var fm = new FileOperationsManager(_config, _tracer);
            return await fm.StartFileEditInSpo(url, user.DisplayName);
        }

        // Deletes lock for document
        // POST: EditActions/ReleaseLock?driveItemId=123
        [HttpPost("[action]")]
        public async Task<ActionResult> ReleaseLock(string driveItemId)
        {
            var driveInfo = await _graphServiceClient.Sites[_config.SharePointSiteId].Drive.Root.Request().GetAsync();

            var fm = new AzureStorageManager(_config, _tracer);
            var locks = await fm.GetLocks(driveInfo.ParentReference.DriveId);
            foreach (var l in locks)
            {
                if (l.RowKey == driveItemId)
                {
                    await fm.ClearLock(l);
                    return Ok();
                }
            }

            return NotFound($"No lock with drive Id '{driveItemId}'");
        }

        // GET: EditActions/GetActiveLocks
        [HttpPost("[action]")]
        public async Task<ActionResult<List<FileLock>>> GetActiveLocks()
        {
            var drive = await _graphServiceClient.Sites[_config.SharePointSiteId].Drive.Request().GetAsync();
            var azManager = new AzureStorageManager(_config, _tracer);

            var initialLocks = await azManager.GetLocks(drive.Id);
            return initialLocks;
        }
    }
}
