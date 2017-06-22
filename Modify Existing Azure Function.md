# How to Update Existing Azure Functions

This tutorial explains how to update existing Azure Functions for sending diagnostic data to AI.

## Prerequisites
1. Your Azure IoT Hub has already been setup 
2. You Have already setup Application Insights
3. Your Azure function is consuming data from IoT Hub or Stream Analytics

If your current environment doesn't meet the prerequisites, you can refer these documents before starting this tutorial: ["Enable E2E Diagnostics in a Full-Stack IoT Hub Solution"](https://github.com/VSChina/iot-hub-e2e-diagnostic/tree/tutorial)

## Update Existing Azure Functions

1. Open your Azure portal, and go to Application Insights. Keep a record of Instrumentation Key shown in the picture below:

![Application Insights Portal](./images/applicationInsights.png)

2. Open Azure Function -> Your Azure Function -> Application settings, add App settings:

|         App Settings Key         |                 App Settings Vaule                  |
|----------------------------------|-----------------------------------------------------|
| APP_INSIGHTS_INSTRUMENTATION_KEY | Instrumentation Key obtained from step 1   |

![Function App Settings 1](./images/Function.png)

3. Open Azure Function -> Your Azure Function -> View files on Right Panel -> Open project.json, add dependencies as below:

```json
{
  "frameworks": {
    "net46":{
      "dependencies": {
        "Microsoft.ApplicationInsights": "2.1.0",
        "WindowsAzure.ServiceBus":"3.4.2"
      }
    }
   }
}
```

4. Insert code in your Azure function to send Telemetry to Application Insights, please note:
- You may need change the parameter name **d2cMessage** to comply with your configuration.
- You need to define your own logic about invalid message criteria

#### Send Azure Function latency to AI
```cs
#r "System.Web.Extensions"
#r "Microsoft.ServiceBus"

using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

public static void Run(EventData d2cMessage, TraceWriter log)
{
    //Your business code here
    ...
    
    //only process messages containing diagnostics property
    const string correlationIdKey = "x-correlation-id";
    if (d2cMessage.Properties.ContainsKey(correlationIdKey))
    {
        DateTime processedTime = DateTime.UtcNow;
        TelemetryClient telemetry = new TelemetryClient();
        telemetry.InstrumentationKey = System.Environment.GetEnvironmentVariable("APP_INSIGHTS_INSTRUMENTATION_KEY");

        const string timeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        var messageData = Encoding.UTF8.GetString(d2cMessage.GetBytes());
        var serializer = new JavaScriptSerializer();
        Dictionary<string, object> messageDictionary = null;
        try
        {
            messageDictionary = serializer.Deserialize<Dictionary<string, object>>(messageData);
        }
        catch
        {
            //ignore error if could not convert to dictionary
        }

        //check whether it is valid message, you should define your own logic about *valid*  
        if(messageDictionary != null && messageDictionary.ContainsKey("temperature"))
        {
            var latencyInMilliseconds = (d2cMessage.EnqueuedTimeUtc - processedTime).TotalMilliseconds;
            latencyInMilliseconds = Math.Max(0, latencyInMilliseconds);
            var properties = new Dictionary<string, string>()
                        {
                            {correlationIdKey, d2cMessage.Properties[correlationIdKey].ToString() },
                            {"EnqueuedTimeUtc", d2cMessage.EnqueuedTimeUtc.ToString(timeFormat) },
                            {"ProcessedTimeUtc", processedTime.ToString(timeFormat) }
                        };

            telemetry.TrackMetric("FunctionLatency", latencyInMilliseconds, properties);
        }
        else
        {
            var customProperties = new Dictionary<string, string>();
            foreach (var keyValue in d2cMessage.Properties)
            {
                customProperties[keyValue.Key] = keyValue.Value.ToString();
            }
            customProperties["MessageBody"] = messageData;
            customProperties["DiagnosticErrorMessage"] = "Fail to read temperature sensor data";
            telemetry.TrackEvent("FunctionInvalidMessage", customProperties);
        }
    }
}
```
