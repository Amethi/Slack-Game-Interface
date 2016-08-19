namespace SlackGameInterface.Lib.Models
{
    public enum GameMessageType
    {
        Playing,
        NotPlaying
    }

    public enum QueueItemOperation
    {
        AddSteamUser,
        RemoveSteamUser,
        Mute,
        UnMute,
        SendTestMessage,
        SendTestGameMessage
    }
}