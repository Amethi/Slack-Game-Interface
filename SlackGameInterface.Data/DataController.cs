using SlackGameInterface.Lib.Exceptions;
using SlackGameInterface.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace SlackGameInterface.Data
{
    public class DataController
    {
        #region members
        private readonly DatabaseContext _db;
        #endregion

        #region constructors
        public DataController()
        {
            _db = new DatabaseContext();
        }
        #endregion

        #region service & diagnostics methods
        public async Task<ServiceConfig> GetServiceConfigAsync()
        {
            var serviceConfig = await _db.ServiceConfigs.FirstOrDefaultAsync();
            if (serviceConfig != null)
                return serviceConfig;

            // service config record doesn't yet exist; create it
            serviceConfig = new ServiceConfig();
            _db.ServiceConfigs.Add(serviceConfig);
            await _db.SaveChangesAsync();
            return serviceConfig;
        }

        public async Task MuteAsync()
        {
            var configService = await GetServiceConfigAsync();
            configService.Silenced = true;
            await _db.SaveChangesAsync();
        }

        public async Task UnMuteAsync()
        {
            var configService = await GetServiceConfigAsync();
            configService.Silenced = false;
            await _db.SaveChangesAsync();
        }
        #endregion

        #region user methods
        public async Task AddSteamUserAsync(string slackUsername, long steamId)
        {
            if (_db.Users.Any(q =>
                q.SlackUsername.Equals(slackUsername, StringComparison.InvariantCultureIgnoreCase) &&
                q.SteamId.Equals(steamId)))
                throw new UserAlreadyAddedException("That user has already been added.");

            var user = new User
            {
                SlackUsername = slackUsername,
                SteamId = steamId
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveSteamUserAsync(string slackUsername)
        {
            var user = await _db.Users.SingleOrDefaultAsync(q => q.SlackUsername.Equals(slackUsername, StringComparison.InvariantCultureIgnoreCase));
            if (user == null)
                throw new UserDoesntExistException("No such Slack user found.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
        #endregion

        #region polling methods
        public async Task UpdateLastPollTimeAsync()
        {
            var serviceConfig = await GetServiceConfigAsync();
            serviceConfig.LastPoll = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _db.Users.ToListAsync();
        }
        #endregion

        /// <summary>
        /// Saves the changes to any database objects retrieved in the same session.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}