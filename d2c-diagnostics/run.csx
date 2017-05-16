#r "Microsoft.ServiceBus"
using System;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

public static void Run(EventData d2cMessage, TraceWriter log)
{
    TelemetryClient telemetry = new TelemetryClient();
    telemetry.InstrumentationKey = System.Environment.GetEnvironmentVariable("APP_INSIGHTS_INSTRUMENTATION_KEY");

    const string sendTimeKey = "x-before-send-request";
    const string correlationIdKey = "x-correlation-id";
    DateTime sendTime;
    if (d2cMessage.Properties.ContainsKey(sendTimeKey)
        && DateTime.TryParse(d2cMessage.Properties[sendTimeKey] as string, out sendTime))
    {
        log.Info(sendTime.ToString());
        var latencyInMilliseconds = (d2cMessage.EnqueuedTimeUtc - sendTime).TotalMilliseconds;
        latencyInMilliseconds = Math.Max(0, latencyInMilliseconds);
        var properties = new Dictionary<string, string>()
                    {
                        {correlationIdKey, d2cMessage.Properties[correlationIdKey].ToString() },
                        {sendTimeKey, d2cMessage.Properties[sendTimeKey].ToString() },
                        {"x-after-receive-request", d2cMessage.EnqueuedTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    };
        log.Info(latencyInMilliseconds.ToString());
        telemetry.TrackMetric("D2CLatency", latencyInMilliseconds, properties);
    }
    else
    {
        var properties = new Dictionary<string, string>
                    {
                        {"Offset", d2cMessage.Offset },
                        {"Data", Encoding.UTF8.GetString(d2cMessage.GetBytes()) }
                    };
        telemetry.TrackEvent("D2CInvalidDiagMsg", properties);
    }
}
