using Microsoft.WindowsAzure.ServiceRuntime;
using SlackGameInterface.Data;
using SlackGameInterface.Lib.Exceptions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace SlackGameInterface.Api.Controllers
{
    /// <summary>
    /// Web API methods that can be called by Slack through custom commands to instruct SGI to do something.
    /// </summary>
    public class CommandsController : ApiController
    {
        /// <summary>
        /// Adds a new user to the system by their slack username.
        /// </summary>
        /// <remarks>
        /// Needs renaming to drop "Steam" from the method name. SGI is to support more than just Steam.
        /// </remarks>
        [HttpPost]
        public async Task<IHttpActionResult> AddSteamUser()
        {
            var token = HttpContext.Current.Request.Form["token"];
            var text = HttpContext.Current.Request.Form["text"];

            if (string.IsNullOrEmpty(token))
                return BadRequest("No token provided.");

            if (token != RoleEnvironment.GetConfigurationSettingValue("Slack.Commands.Add-Steam-User.VerificationToken"))
                return Unauthorized();

            if (string.IsNullOrEmpty(text))
                return BadRequest("No Slack username or Steam id provided.");

            if (!text.Contains(" "))
                return BadRequest("Two parameters are needed: The Slack username and Steam id of the person to add.");

            var textParts = text.Split(' ');
            if (textParts.Length != 2)
                return BadRequest("Two parameters are needed: The Slack username and Steam id of the person to add.");

            var slackUsername = textParts[0];
            long steamId;
            if (!long.TryParse(textParts[1], out steamId))
                return BadRequest("The steam-id parameter did not appear to be a valid number.");

            try
            {
                await new DataController().AddSteamUserAsync(slackUsername, steamId);
            }
            catch (UserAlreadyAddedException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok("Steam user added.");
        }

        /// <summary>
        /// Removes a user from SGI by their slack username.
        /// </summary>
        /// <remarks>
        /// Needs renaming to drop "Steam" from the method name. SGI is to support more than just Steam.
        /// </remarks>
        [HttpPost]
        public async Task<IHttpActionResult> RemoveSteamUser()
        {
            var token = HttpContext.Current.Request.Form["token"];
            var slackUsername = HttpContext.Current.Request.Form["text"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("No token provided.");

            if (token != RoleEnvironment.GetConfigurationSettingValue("Slack.Commands.Remove-Steam-User.VerificationToken"))
                return Unauthorized();

            if (string.IsNullOrEmpty(slackUsername))
                return BadRequest("No Slack username provided.");

            try
            {
                await new DataController().RemoveUserAsync(slackUsername);
            }
            catch (UserDoesntExistException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok("Steam user removed.");
        }

        /// <summary>
        /// Causes SGI to be muted, so it doesn't post into Slack.
        /// </summary>
        [HttpPost]
        public async Task<IHttpActionResult> Mute()
        {
            var token = HttpContext.Current.Request.Form["token"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("No token provided.");

            if (token != RoleEnvironment.GetConfigurationSettingValue("Slack.Commands.Mute.VerificationToken"))
                return Unauthorized();

            await new DataController().MuteAsync();
            return Ok("Muted.");
        }

        /// <summary>
        /// Causes SGI to be un-muted, so it can continue to post into Slack.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IHttpActionResult> UnMute()
        {
            var token = HttpContext.Current.Request.Form["token"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("No token provided.");

            if (token != RoleEnvironment.GetConfigurationSettingValue("Slack.Commands.UnMute.VerificationToken"))
                return Unauthorized();

            await new DataController().UnMuteAsync();
            return Ok("Un-muted.");
        }
    }
}
