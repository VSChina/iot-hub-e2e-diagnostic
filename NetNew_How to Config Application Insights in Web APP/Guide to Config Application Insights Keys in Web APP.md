# How to config Application Insights API Keys in Web APP

With the deployment of net new ARM Template finish, there would be all the necessary resources for end to end diagnostics, including the Web APP service and the Application Insights.

In order to track the behaviors of Web APP correctly with Application Insights, there are some required settings need to be configured manually.

First of all, *Application ID* of the Application Insights must be set in the Web APP. Besides, there are several API Keys the Web APP need to use.

## Get the Application ID

Find the Application Insights deployed by the ARM Template, click **API Access** in the left panel under **CONFIGURE**, the *Application ID* just shows in right panel.

Copy it and we will use it later.

![Application ID](./applicationid.png)

## Create Application Insights API Keys

Stay in the **API Access** panel, click the **Create API Key** button:

![Create API Key](./create_api_key.png)

Provide a description in the pop up panel to help you identify this API Key in the future.
Check all permission that the API Key will allow apps to do.

Then click **Generate key**.

**Make sure you copy the key immediately.** You won't see it again once you close the panel.

Repeat the actions several times depending on the number of keys requried (We need 8 API Key for our scenario). And please make sure you copy the API Key immediately after it is generated.

![Fill in the panel](./description.png)

## Config the API Key in Web APP

Go to the Web APP that will leverage the Application Insights, click the **Application settings** in the left panel, scroll down to App Settings.
Fill in the rows whose keys were already set by ARM Template with corresponding values, including the Application ID we just copied and the API Keys created.

Click the **Save** button in the left top to save all the settings.

![Configure Web APP](./set_api_key.png)




