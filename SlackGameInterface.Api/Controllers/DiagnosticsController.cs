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
    public class DiagnosticsController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Ping()
        {
            return Ok("Pong: " + DateTime.Now);
        }

        [HttpGet]
        [ResponseType(typeof(ServiceConfig))]
        public async Task<IHttpActionResult> GetServiceConfig()
        {
            var serviceConfig = await new DataController().GetServiceConfigAsync();
            if (serviceConfig != null)
                return Ok(serviceConfig);

            return NotFound();
        }

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
