# Enable E2E Diagnostics in a Full-Stack IoT Hub Solution

This tutorial will demonstrate how to enable end-to-end diagnostics in a full-stack IoT Hub solution.

In this tutorial, you will learn:
* IoT Hub solution architecture
* How to setup E2E diagnostics based on your existing IoT solution
* How to verify E2E diagnostics works

## Prerequisite
We suppose you have Azure account already, if not, please first [create an azure account](https://azure.microsoft.com/en-us/free/).

## Full-Stack IoT Hub Solution Architecture
The following figure gives one typical IoT Hub solution architecture:

   ![](./images/IoTHubSolution.png)
To enable end-to-end diagnostics in the above IoT Hub solution, a few resources need to be provisioned. The following figure demonstrates new solution architecture with end-to-end diagnostics support:

   ![](./images/IoTHubSolution_E2EDiag.png)

## Provision Necessary Resources to Support E2E Diagnostics
Based on your current IoT solution, you could follow any of following steps to setup E2E diagnostics:
- [Setup E2E Diagnostics solution from scratch](./Guide%20to%20Setup%20E2E%20Diagnostics%20Solution%20from%20Scratch.md)
- [Setup E2E Diagnostics solution with existing IoT Hub and Stream Analytics](./Guide%20to%20Setup%20E2E%20Diagnostics%20Solution%20With%20Existing%20IoT%20Hub%20and%20Stream%20Analytics.md)
- [Setup E2E Diagnostics solution with existing IoT Hub and Function App](./Guide%20to%20Setup%20E2E%20Diagnostics%20Solution%20with%20Existing%20IoT%20Hub%20and%20Function%20App.md)
- [Setup E2E Diagnostics Solution with existing Application Insights](./Guide%20to%20Setup%20E2E%20Diagnostics%20Solution%20With%20Existing%20Application%20Insights.md)

## Verify E2E Diagnostics

After setting up E2E diagnostics, there are several steps to do to verify if it works as expected.

### Create At Least One Device In IoT Hub

### Send D2C Messages Using E2E Diagnostics Layered Azure IoT SDKs
There are three available layered SDKs, you could choose any of them to send D2C messages:
- [Azure IoT E2E Diagnostics Layered SDK for .NET](https://github.com/VSChina/azure-iot-diagnostics-csharp)
- [Azure IoT E2E Diagnostics Layered SDK for C](https://github.com/erich-wang/azure-iot-sdk-c/tree/e2e-diag)
- [Azure IoT E2E Diagnostics Layered SDK for JAVA](https://github.com/VSChina/azure-iot-diagnostics-java)

For demo purpose, you may follow the steps below to use your computer as a simulate device with .NET SDK:
1. Get source code
```
git clone https://github.com/VSChina/azure-iot-diagnostics-csharp.git
git checkout bugbash
```
2. If you have VS 2017 installed already, you could just open DeviceSDKWrapper.sln with it, get the deviceConnectionString from azure portal and fill it in Sample project app.config to run
3. If not, you could unzip Sample/Sample.zip, update deviceConnectionString in Sample.exe.config with the value get from azure portal, then run Sample.exe

### Start Stream Analytics Job
This step is necessary only if Stream Analytics is included in the IoT solution.
Among all resources deployed by ARM template, there is one Stream Analytics job resource starting with **stream**. Open this resource and navigate to its Overview tab, click start button to start the streaming job.

### Check Dashboard
Among all resources deployed by ARM template, there is one App Service resource starting with **webapp**, find the App Service first, then navigate to its Overview tab, make a note of the **URL** value.

   ![](images/Dashboard.png)
Open the url, switch to "Diagnostics Map", the number of messages should be 0.

### Update Device Sampling Rate
1. Stay in the page, navigate back to **Home**
2. Set 'Status' ON and 'Sample' value between 0-100

   ![](./images/Configure_Sample.png)
3. 'Device List' is optional, leave it blank to update all devices, or set the value 'device1,device2,device3' in this format to update certain devices

### Check Dashboard
Connected device value should be updated to 1 in 3-5 minutes. Failed message percentage should be 0 and the number of messages processed should be greater than 0.

### Failed Messages Percentage
**Failed messages** is a business logic concept, for example, we could treat a message as invalid if it misses the required fileds or the filed value is not as expected.
In our sample solution, we treat a message as invalid if 'temperature' field is missing. You could re-define the Azure Function per your business need.
