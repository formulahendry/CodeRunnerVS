using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;

namespace CodeRunner
{
    static class AppInsightsClient
    {
        static readonly TelemetryClient telemetry;

        static AppInsightsClient()
        {
            telemetry = new TelemetryClient();
            telemetry.InstrumentationKey = "a30bcc73-ded9-46f2-b664-c6ed415bd393";
        }

        public static void trackEvent(string eventName)
        {
            telemetry.TrackEvent(eventName);
        }
    }
}
