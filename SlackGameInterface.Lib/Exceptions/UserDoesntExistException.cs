using System;

namespace SlackGameInterface.Lib.Exceptions
{
    public class UserDoesntExistException : Exception
    {
        public UserDoesntExistException(string message) : base(message)
        {
        }
    }
}
