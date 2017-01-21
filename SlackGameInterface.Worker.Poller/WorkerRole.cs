using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.ServiceRuntime;
using Slack.Webhooks;
using SlackGameInterface.Data;
using SlackGameInterface.Lib.Models;
using SteamWebAPI2.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SlackGameInterface.Worker.Poller
{
    /// <summary>
    /// Polls remote systems such as game API's for changes to our users and posts anything relevant to Slack.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        #region members
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private readonly TelemetryClient _telemetryClient = new TelemetryClient();
        #endregion

        #region service methods
        public override void Run()
        {
            Trace.TraceInformation("SlackGameInterface.Worker.Poller is running");

            try
            {
                RunAsync(_cancellationTokenSource.Token).Wait();
            }
            catch (Exception ex)
            {
                // ensure any exceptions are persisted to Azure Application Insights.
                _telemetryClient.TrackException(ex);
            }
            finally
            {
                _runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            var result = base.OnStart();

            Trace.TraceInformation("SlackGameInterface.Worker.Poller has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("SlackGameInterface.Worker.Poller is stopping");

            _cancellationTokenSource.Cancel();
            _runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("SlackGameInterface.Worker.Poller has stopped");
        }

        private static async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                // at the moment we are only polling Steam for information.
                await PollSteamAsync();

                // but we could add in more game api's here....

                await new DataController().UpdateLastPollTimeAsync();

                // pause before the next poll to ensure we don't exceed any rate limits on APIs.
                var interval = TimeSpan.FromSeconds(int.Parse(RoleEnvironment.GetConfigurationSettingValue("Poller.IntervalPeriodSeconds")));
                await Task.Delay(interval, cancellationToken);
            }
        }
        #endregion

        #region game api polling methods
        /// <summary>
        /// Polls the Steam API for changes against our known SGI users.
        /// </summary>
        private static async Task PollSteamAsync()
        {
            // get steam user profiles to see if there's any updates, i.e. people playing games
            var dataController = new DataController();
            var users = await dataController.GetUsersAsync();
            if (users.Count == 0)
            {
                Trace.WriteLine("No users to poll. Not polling.");
                return;
            }

            Trace.WriteLine($"Got {users.Count} users to query.");
            var ids = users.Select(user => user.SteamId).ToList();
            var steam = new SteamUser(RoleEnvironment.GetConfigurationSettingValue("Steam.Key"));
            var summaries = await steam.GetPlayerSummariesAsync(ids);
            var gameMessages = new List<GameMessage>();
            Trace.WriteLine($"Got {summaries.Count} Steam player summaries.");

            foreach (var summary in summaries)
            {
                var user = users.Single(q => q.SteamId.ToString().Equals(summary.SteamId));
                if (!string.IsNullOrEmpty(summary.PlayingGameId) && string.IsNullOrEmpty(user.SteamGameId))
                {
                    // user has started playing
                    user.SteamGameId = summary.PlayingGameId;
                    user.SteamGameName = summary.PlayingGameName;
                    user.SteamLastTimePlaying = DateTime.Now;
                    Trace.WriteLine($"{user.SlackUsername} has started playing {summary.PlayingGameName}.");

                    // have we already got users playing this game this poll?
                    var gameMessage = gameMessages.SingleOrDefault(q => q.GameId.Equals(user.SteamGameId) && q.Type == GameMessageType.Playing);
                    if (gameMessage != null)
                        gameMessage.Users.Add(user);
                    else
                    {
                        gameMessage = new GameMessage { GameId = user.SteamGameId, GameName = user.SteamGameName, Type = GameMessageType.Playing };
                        gameMessage.Users.Add(user);
                        gameMessages.Add(gameMessage);
                    }
                }
                else if (string.IsNullOrEmpty(summary.PlayingGameId) && !string.IsNullOrEmpty(user.SteamGameId))
                {
                    // user has stopped playing - don't create a message, it can get a little spammy if we notify when
                    // someone starts AND stops playing a game.

                    Trace.WriteLine($"{user.SlackUsername} has stopped playing {summary.PlayingGameName}.");
                    user.SteamGameId = null;
                    user.SteamGameName = null;
                    
                }
                else if (summary.PlayingGameId != user.SteamGameId)
                {
                    // user is still playing, but has changed game since we last polled Steam
                    user.SteamGameId = summary.PlayingGameId;
                    user.SteamGameName = summary.PlayingGameName;
                    user.SteamLastTimePlaying = DateTime.Now;
                    Trace.WriteLine($"{user.SlackUsername} has changed game to {summary.PlayingGameName}.");

                    // have we already got users playing this game this poll?
                    var gameMessage = gameMessages.SingleOrDefault(q => q.GameId.Equals(user.SteamGameId) && q.Type == GameMessageType.Playing);
                    if (gameMessage != null)
                        gameMessage.Users.Add(user);
                    else
                    {
                        gameMessage = new GameMessage { GameId = user.SteamGameId, GameName = user.SteamGameName, Type = GameMessageType.Playing };
                        gameMessage.Users.Add(user);
                        gameMessages.Add(gameMessage);
                    }
                }
            }

            // save game state change back to the database
            await dataController.SaveChangesAsync();

            // now send any messages we've built up
            await SendRichGameMessagesAsync(gameMessages);
        }
        #endregion

        #region message methods
        /// <summary>
        /// Prepares a message for posting in Slack by ensuring it has all the common details set.
        /// </summary>
        private static SlackMessage NewSlackMessage()
        {
            var slackChannel = RoleEnvironment.GetConfigurationSettingValue("Slack.Channel");
            var username = RoleEnvironment.GetConfigurationSettingValue("Slack.Bot.Username");
            var iconUrl = RoleEnvironment.GetConfigurationSettingValue("Slack.Bot.Icon-Url");
            
            return new SlackMessage
            {
                Channel = slackChannel,
                Username = username,
                IconUrl = new Uri(iconUrl),
            };
        }

        ///// <summary>
        ///// Send a single message with rich formatting into Slack for each of our game messages.
        ///// </summary>
        ///// <param name="gameMessages">The list of game messages to send to Slack.</param>
        //private static async Task SendRichGameMessagesAsync(IReadOnlyCollection<GameMessage> gameMessages)
        //{
        //    // work out what kind of message(s) we're going to send to slack, if any
        //    var serviceConfig = await new DataController().GetServiceConfigAsync();
        //    if (gameMessages.Count > 0 && !serviceConfig.Silenced)
        //    {
        //        var webhookUrl = RoleEnvironment.GetConfigurationSettingValue("Slack.Webhook-Url");
        //        var slack = new SlackClient(webhookUrl);
        //        foreach (var gameMessage in gameMessages)
        //        {
        //            var message = NewSlackMessage();
        //            var attachment = new SlackAttachment
        //            {
        //                ImageUrl = $"http://cdn.akamai.steamstatic.com/steam/apps/{gameMessage.GameId}/capsule_sm_120.jpg",
        //                Color = "#FFB400",
        //                MrkdwnIn = new List<string> { "text" }
        //            };

        //            if (gameMessage.Type != GameMessageType.Playing)
        //                continue;

        //            switch (gameMessage.Users.Count)
        //            {
        //                case 1:
        //                    attachment.Text = $"*{gameMessage.Users[0].SlackUsername}* is now playing <http://store.steampowered.com/app/{gameMessage.GameId}/|*{gameMessage.GameName}*>";
        //                    break;
        //                case 2:
        //                    attachment.Text = $"*{gameMessage.Users[0].SlackUsername}* and *{gameMessage.Users[1].SlackUsername}* are now playing <http://store.steampowered.com/app/{gameMessage.GameId}/|*{gameMessage.GameName}*>";
        //                    break;
        //                default:
        //                    if (gameMessage.Users.Count > 2)
        //                    {
        //                        var firstUsers = string.Join("*, *", gameMessage.Users.GetRange(0, gameMessage.Users.Count - 1).Select(q => q.SlackUsername));
        //                        attachment.Text = $"*{firstUsers}* and *{gameMessage.Users.Last().SlackUsername}* are all now playing <http://store.steampowered.com/app/{gameMessage.GameId}/|*{gameMessage.GameName}*>! Get in!";
        //                    }
        //                    break;
        //            }

        //            message.Attachments = new List<SlackAttachment> { attachment };
        //            var result = slack.Post(message);
        //            Trace.WriteLine($"Posting message to slack was successful? {result}");
        //        }
        //    }
        //}

        /// <summary>
        /// Send a single message with rich formatting into Slack for each of our game messages.
        /// </summary>
        /// <param name="gameMessages">The list of game messages to send to Slack.</param>
        private static async Task SendRichGameMessagesAsync(IReadOnlyCollection<GameMessage> gameMessages)
        {
            // work out what kind of message(s) we're going to send to slack, if any
            var serviceConfig = await new DataController().GetServiceConfigAsync();
            if (gameMessages.Count > 0 && !serviceConfig.Silenced)
            {
                var webhookUrl = RoleEnvironment.GetConfigurationSettingValue("Slack.Webhook-Url");
                var slack = new SlackClient(webhookUrl);
                foreach (var gameMessage in gameMessages)
                {
                    var message = NewSlackMessage();
                    if (gameMessage.Type != GameMessageType.Playing)
                        continue;

                    switch (gameMessage.Users.Count)
                    {
                        case 1:
                            message.Text = $"*{gameMessage.Users[0].SlackUsername}* is now playing <http://store.steampowered.com/app/{gameMessage.GameId}/|*{gameMessage.GameName}*>";
                            break;
                        case 2:
                            message.Text = $"*{gameMessage.Users[0].SlackUsername}* and *{gameMessage.Users[1].SlackUsername}* are now playing <http://store.steampowered.com/app/{gameMessage.GameId}/|*{gameMessage.GameName}*>";
                            break;
                        default:
                            if (gameMessage.Users.Count > 2)
                            {
                                var firstUsers = string.Join("*, *", gameMessage.Users.GetRange(0, gameMessage.Users.Count - 1).Select(q => q.SlackUsername));
                                message.Text = $"*{firstUsers}* and *{gameMessage.Users.Last().SlackUsername}* are all now playing <http://store.steampowered.com/app/{gameMessage.GameId}/|*{gameMessage.GameName}*>! Get in!";
                            }
                            break;
                    }

                    var result = slack.Post(message);
                    Trace.WriteLine($"Posting message to slack was successful? {result}");
                }
            }
        }
        #endregion
    }
}