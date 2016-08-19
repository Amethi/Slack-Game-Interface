using SlackGameInterface.Lib.Exceptions;
using SlackGameInterface.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace SlackGameInterface.Data
{
    /// <summary>
    /// Data Abstraction Layer that is for sharing between the Web Api and Worker roles. 
    /// Performs all operations with the Azure SQL database.
    /// </summary>
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
        /// <summary>
        /// Adds a new user to SGI with their Steam ID.
        /// Needs an accompanying method so that it can add Steam details to an existing user.
        /// </summary>
        /// <param name="slackUsername">The username of the person in Slack</param>
        /// <param name="steamId">The Steam id for the user.</param>
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

        /// <summary>
        /// Removes a user from SGI.
        /// </summary>
        /// <param name="slackUsername">The username of the person in Slack to remove.</param>
        public async Task RemoveUserAsync(string slackUsername)
        {
            var user = await _db.Users.SingleOrDefaultAsync(q => q.SlackUsername.Equals(slackUsername, StringComparison.InvariantCultureIgnoreCase));
            if (user == null)
                throw new UserDoesntExistException("No such Slack user found.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
        #endregion

        #region polling methods
        /// <summary>
        /// Updates the ServiceConfig record with the time worker polling was last run.
        /// </summary>
        public async Task UpdateLastPollTimeAsync()
        {
            var serviceConfig = await GetServiceConfigAsync();
            serviceConfig.LastPoll = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all of the users in SGI.
        /// </summary>
        public async Task<List<User>> GetUsersAsync()
        {
            return await _db.Users.ToListAsync();
        }
        #endregion

        /// <summary>
        /// Saves the changes to any database objects retrieved from the current class instance.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}