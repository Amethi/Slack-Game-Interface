using System;
using System.ComponentModel.DataAnnotations;

namespace SlackGameInterface.Lib.Models
{
    public class User
    {
        [Key]
        public string SlackUsername { get; set; }

        #region Steam
        /// <summary>
        /// The unique-identifier for the user in Steam.
        /// </summary>
        public long SteamId { get; set; }

        /// <summary>
        /// If the user is playing a game currently, this will be the Steam id for it.
        /// A null value means the user is not playing a game.
        /// </summary>
        public string SteamGameId { get; set; }

        /// <summary>
        /// If the user is playing a game currently, this will be its name. 
        /// A null value means the user is not playing a game.
        /// </summary>
        public string SteamGameName { get; set; }

        ///// <summary>
        ///// The ip and port of the game server the user is currently playing on, if they are playing on-line in a game using Steam matchmaking. 
        ///// Otherwise will be set to "0.0.0.0:0".
        ///// A null value means the user is not playing a game.
        ///// </summary>
        //public string SteamGameServerIp { get; set; }

        /// <summary>
        /// The last time a user was observed to be playing a game.
        /// This time will not be precise as it depends how often the agent checks the Steam API.
        /// </summary>
        public DateTime? SteamLastTimePlaying { get; set; }
        #endregion

        //#region Overwatch
        ///// <summary>
        ///// The users username in Battle.net.
        ///// </summary>
        //public string BattleNetUsername { get; set; }
        //#endregion
    }
}
