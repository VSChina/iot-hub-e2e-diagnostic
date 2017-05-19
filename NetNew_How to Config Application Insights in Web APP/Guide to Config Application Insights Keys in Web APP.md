# How to config application insights api keys in web app

With the deployment of net new arm template finish, there would be all the necessary resources for end to end diagnostics, including the web app service and the application insights.

In order to track the behaviors of web app correctly with application insights, there are some required settings need to be configured manually.

First of all, *Application ID* of the application insights must be set in the web app. Besides, there are several api keys the web app need to use.

## Get the application id

Find the application insights deployed by the arm template, click **API Access** in the left panel under **CONFIGURE**, the *Application ID* just shows in right panel.

Copy it and we will use it later.

![Application ID](./applicationid.png)

## Create application insights api keys

Stay in the **API Access** panel, click the **Create API Key** button:

![Create API Key](./create_api_key.png)

Provide a description in the pop up panel to help you identify this API key in the future.
Check all permission that the API key will allow apps to do.

Then click **Generate key**.

**Make sure you copy the key immediately.** You won't see it again once you close the panel.

Repeat the actions several times depending on the number of keys requried. And please make sure you copy the api key immediately after it is generated.

![Fill in the panel](./description.png)

## Config the API Key in Web APP

Go to the web app that will leverage the application insights, click the **Application settings** in the left panel, scroll down to App Settings.
Fill in the rows whose keys were already set by arm template with corresponding values, including the application id we just copied and the api keys created.

Click the **Save** button in the left top to save all the settings.

![Configure Web APP](./set_api_key.png)




