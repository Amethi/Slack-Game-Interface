using Microsoft.ApplicationInsights;
using System.Web.Http.ExceptionHandling;

namespace SlackGameInterface.Api
{
    public class AiExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            if (context?.Exception != null)
            {
                //or reuse instance (recommended!). see note above 
                var ai = new TelemetryClient();
                ai.TrackException(context.Exception);
            }

            base.Log(context);
        }
    }
}