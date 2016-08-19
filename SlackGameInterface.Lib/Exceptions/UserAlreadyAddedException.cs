using System;

namespace SlackGameInterface.Lib.Exceptions
{
    public class UserAlreadyAddedException : Exception
    {
        public UserAlreadyAddedException(string message) : base(message)
        {
        }
    }
}
