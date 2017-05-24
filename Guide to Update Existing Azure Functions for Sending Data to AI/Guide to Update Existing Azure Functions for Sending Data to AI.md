# How to Update Existing Azure Functions

This tutorial explains how to update existing Azure Functions for sending diagnostic data to AI.

## Prerequisites
1. Your Azure IoT Hub has already been setup 
2. You Have already setup Application Insights
3. Your Event Hub has already been setup and consume data from IoT Hub and Stream Analytics

If your current environment doesn't meet the prerequisites, you can refer these documents before starting this tutorial: ["Guide to Update Existing IoT Hub and SAS  / Guide to Config Application Insights Keys in Web APP"](https://github.com/VSChina/iot-hub-e2e-diagnostic/tree/tutorial)

## Update Existing Azure Functions

1. Open your Azure portal, and go to Application Insights. Keep a record of Instrumentation Key shown in the picture below:

![Application Insights Portal](./applicationInsights.png)


2. Get Event Hub connection string from Azure Portal: Event Hub -> Shared Access Policies -> RootManageSharedAccessKey

![Event Hub Settings](./EventHub.png)


3. Open Azure Function -> Your Azure Function -> Application settings, add App settings:

|         App Settings Key         |                 App Settings Vaule                  |
|----------------------------------|-----------------------------------------------------|
| APP_INSIGHTS_INSTRUMENTATION_KEY | Instrumentation Key obtained from step 1   |
| EVENTHUB_CONN_STRING             | Event Hub Connection String Obtained from step 2 |


![Function App Settings 1](./Function.png)


4. Config your Azure Function Triggers to Azure Event Hub, and the Event Hub connection is EVENTHUB_CONN_STRING

![Function App Settings 2](./FunctionSettings.png)


5. Upate your function.json files
#### For D2C Message Latency
```json
{
  "bindings": [
    {
      "type": "eventHubTrigger",
      "name": "d2cMessage",
      "direction": "in",
      "path": "d2c-diagnostics",
      "connection": "DIAG_ENDPOINT",
      "consumerGroup": "$Default"
    }
  ],
  "disabled": false
}
```

#### For Stream Analytics Latency
```json
{
  "bindings": [
    {
      "type": "eventHubTrigger",
      "name": "d2cMessage",
      "direction": "in",
      "path": "stream-diagnostics",
      "connection": "DIAG_ENDPOINT",
      "consumerGroup": "$Default"
    }
  ],
  "disabled": false
}
```

6. Insert code to send Telemetry to Application Insights

#### Send D2C message latency to AI
```cs
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
```


#### Send stream analytics latency to AI

```cs
    TelemetryClient telemetry = new TelemetryClient();
    telemetry.InstrumentationKey = System.Environment.GetEnvironmentVariable("APP_INSIGHTS_INSTRUMENTATION_KEY");

    const string enqueueTimeKey = "EnqueuedTime";
    const string processedTimeKey = "processed-utc-time";
    const string correlationIdKey = "x-correlation-id";
    const string iotHubKey = "IoTHub";

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
        }
    }
```