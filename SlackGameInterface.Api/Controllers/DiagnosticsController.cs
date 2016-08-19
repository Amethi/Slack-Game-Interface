using Microsoft.WindowsAzure.ServiceRuntime;
using Slack.Webhooks;
using SlackGameInterface.Data;
using SlackGameInterface.Lib.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace SlackGameInterface.Api.Controllers
{
    /// <summary>
    /// Diagnostic Web API methods that can be requested through the browser.
    /// </summary>
    public class DiagnosticsController : ApiController
    {
        /// <summary>
        /// Useful for telling if the service is up.
        /// </summary>
        [HttpGet]
        public IHttpActionResult Ping()
        {
            return Ok("Pong: " + DateTime.Now);
        }

        /// <summary>
        /// Useful for seeing if the database is initialised.
        /// </summary>
        [HttpGet]
        [ResponseType(typeof(ServiceConfig))]
        public async Task<IHttpActionResult> GetServiceConfig()
        {
            var serviceConfig = await new DataController().GetServiceConfigAsync();
            if (serviceConfig != null)
                return Ok(serviceConfig);

            return NotFound();
        }

        /// <summary>
        /// Sends a test message into Slack.
        /// </summary>
        [HttpGet]
        public async Task<IHttpActionResult> SendTestMessage()
        {
            var slack = new SlackClient(RoleEnvironment.GetConfigurationSettingValue("Slack.Webhook-Url"));
            var message = new SlackMessage
            {
                Channel = RoleEnvironment.GetConfigurationSettingValue("Slack.Test-Channel"),
                Username = RoleEnvironment.GetConfigurationSettingValue("Slack.Bot.Username"),
                IconUrl = new Uri(RoleEnvironment.GetConfigurationSettingValue("Slack.Bot.Icon-Url")),
                Text = "This is a test announcement. Do not be alarmed."
            };

            await slack.PostAsync(message);
            return Ok("Test message sent.");
        }

        /// <summary>
        /// Sends a test game message into Slack.
        /// </summary>
        [HttpGet]
        public async Task<IHttpActionResult> SendTestGameMessage()
        {
            var slack = new SlackClient(RoleEnvironment.GetConfigurationSettingValue("Slack.Webhook-Url"));
            var message = new SlackMessage
            {
                Channel = RoleEnvironment.GetConfigurationSettingValue("Slack.Test-Channel"),
                Username = RoleEnvironment.GetConfigurationSettingValue("Slack.Bot.Username"),
                IconUrl = new Uri(RoleEnvironment.GetConfigurationSettingValue("Slack.Bot.Icon-Url")),
            };

            var attachment = new SlackAttachment
            {
                ImageUrl = "http://cdn.akamai.steamstatic.com/steam/apps/379720/capsule_sm_120.jpg",
                Text = "*jay* is now playing <http://store.steampowered.com/app/379720/|*DOOM*>",
                Color = "#FFB400",
                MrkdwnIn = new List<string> { "text" }
            };

            message.Attachments = new List<SlackAttachment> { attachment };
            await slack.PostAsync(message);
            return Ok("Test game message sent.");
        }
    }
}
