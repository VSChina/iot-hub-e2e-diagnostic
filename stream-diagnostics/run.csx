#r "System.Web.Extensions"
#r "Newtonsoft.Json"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System.Web.Script.Serialization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

public static void Run(EventData d2cMessage, TraceWriter log, ICollector<E2EItem> latencyTable, ICollector<E2EItem> errorTable)
{
    TelemetryClient telemetry = new TelemetryClient();
    telemetry.InstrumentationKey = System.Environment.GetEnvironmentVariable("APP_INSIGHTS_INSTRUMENTATION_KEY");

    const string enqueueTimeKey = "EnqueuedTime";
    const string processedTimeKey = "processed-utc-time";
    const string correlationIdKey = "x-correlation-id";
    const string iotHubKey = "IoTHub";

    DateTime epochTime = new DateTime(1970, 1, 1);
    bool validMessage = false;
    var message = Encoding.UTF8.GetString(d2cMessage.GetBytes());
    var serializer = new JavaScriptSerializer();
    var propertiesArray = serializer.Deserialize<Dictionary<string, object>[]>(message);

    foreach(var properties in propertiesArray) {
        DateTime enqueueTime, processedTime = DateTime.MinValue;
        if (properties.ContainsKey(correlationIdKey) &&
            properties.ContainsKey(processedTimeKey) &&
            properties.ContainsKey(iotHubKey) &&
            properties.ContainsKey("temperature") &&
            DateTime.TryParse(properties[processedTimeKey].ToString(), out processedTime))
        {
            var iotProperties = properties[iotHubKey] as Dictionary<string, object>;
            if (iotProperties.ContainsKey(enqueueTimeKey) &&
                DateTime.TryParse(iotProperties[enqueueTimeKey].ToString(), out enqueueTime))
            {
                var latencyInMilliseconds = (processedTime - enqueueTime).TotalMilliseconds;
                log.Info(iotProperties[enqueueTimeKey].ToString());
                log.Info(properties[processedTimeKey].ToString());
                latencyInMilliseconds = Math.Max(0, latencyInMilliseconds);
                var customeProperties = new Dictionary<string, string>()
                            {
                                {correlationIdKey, properties[correlationIdKey].ToString()},
                                {enqueueTimeKey, iotProperties[enqueueTimeKey].ToString()},
                                {processedTimeKey, properties[processedTimeKey].ToString()}
                            };

                telemetry.TrackMetric("StreamJobLatency", latencyInMilliseconds, customeProperties);
                latencyTable.Add(new E2EItem
                {
                    PartitionKey = ((int)(DateTime.UtcNow - epochTime).TotalSeconds).ToString(),
                    RowKey = Guid.NewGuid().ToString(),
                    DiagName = "StreamJobLatency",
                    Latency = latencyInMilliseconds,
                    Properties = properties
                });

                validMessage = true;
            }
        }
        if (!validMessage)
        {
            var customProperties = new Dictionary<string, string>();
            foreach (var keyValue in properties)
            {
                if (keyValue.Key == iotHubKey || keyValue.Key == "User")
                {
                    var iotProperties = keyValue.Value as Dictionary<string, object>;
                    foreach (var iotKV in iotProperties)
                    {
                        customProperties[keyValue.Key + "." + iotKV.Key] = iotKV.Value == null ? "null" : iotKV.Value.ToString();
                    }
                }
                else
                {
                    customProperties[keyValue.Key] = keyValue.Value.ToString();
                }
            }
            customProperties["DiagnosticErrorMessage"] = "Fail to read temperature sensor data";
            telemetry.TrackMetric("StreamInvalidMessage", 1, customProperties);
            errorTable.Add(new E2EItem
            {
                PartitionKey = ((int)(DateTime.UtcNow - epochTime).TotalSeconds).ToString(),
                RowKey = Guid.NewGuid().ToString(),
                DiagName = "StreamInvalidMessage",
                Latency = 0,
                Properties = customProperties
            });
        }
    }
}

public class E2EItem
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string DiagName {get; set; }
    public int Latency{get; set;}
    public Dictionary<string, string> Properties {get; set;}
}
