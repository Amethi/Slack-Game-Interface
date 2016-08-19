using System.Collections.Generic;

namespace SlackGameInterface.Lib.Models
{
    /// <summary>
    /// Allows aggregation of user game data in preparation for sending to Slack.
    /// </summary>
    public class GameMessage
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public List<User> Users { get; set; }
        public GameMessageType Type { get; set; }

        public GameMessage()
        {
            Users = new List<User>();
        }
    }
}
