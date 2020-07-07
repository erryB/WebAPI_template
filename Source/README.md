# Web API Code

[[_TOC_]]

## Prerequisites
Instructions on how to deploy required infrastructure and how to configure deployed services can be found in [main README, section Getting Started](..\README.md).

## Configuring appsettings for development

Copy the appsettings.json shown below in appsettings.Development.json and replace the placeholder with your own values.

```
{
  "AuthScheme": "AzureAdB2B",
  "AzureAdB2C": {
    "Instance": "https://{your_aad_b2c_account}.b2clogin.com",
    "ClientId": "{your_aad_b2c_app_id}",
    "Domain": "{your_aad_b2c_account}.onmicrosoft.com",
    "SignUpSignInPolicyId": "{your_aad_b2c_signup_signin_flow}"
  },
  "AzureAdB2B": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "{your_aad_account}.onmicrosoft.com",
    "TenantId": "{your_aad_tenant_id}",
    "ClientId": "{your_aad_app_id}"
  },
  "InviteLandingPage": "{your_landing_page}",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:{server_name},1433;Database={db_name};"
  },
  /*
  To connect to the SQL database, you will need the credentials of the managed identity for the backend application.
  If developing locally, set ConnectionOption to "RunAs=Developer; DeveloperTool=VisualStudio".
  More information on MSI w/ App Service and SQL DB here: https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-connect-msi
  More information on connection options here: https://docs.microsoft.com/en-us/azure/key-vault/general/service-to-service-authentication#connection-string-support
  */
  "ManagedIdentity": {
    "ConnectionOption": "RunAs=App;",
    "TenantId": "{tenant_id}"
  },
  "AllowedHosts": "*",
  "ApplicationInsights": {
    "InstrumentationKey": "{appinsights_key}"
  }
}
```

If you are using **B2C Auth** then you can find the values here:

`{your_aad_b2c_account}` can be found in your Azure B2C Directory > App registrations > Endpoints.
`{your_aad_b2c_app_id}` can be found in your Azure B2C Directory > App registrations > your app > Overview.
`{your_aad_b2c_signup_signin_flow}` can be found in your Azure B2C Directory > User Flows.

If you are using **B2B Auth** then you can find the values here:

`{your_aad_account}` and `{your_aad_tenant_id}` can be found in your Azure Active Directory > Tenant information.
`{your_aad_app_id}` can be found in your Azure Active Directory > App registrations > your app > Overview.

`ConnectionString` and `ManagedIdentity` sections are used to connect to the **Azure SQL Database** safely with Managed Identities, which doesn't require us to specify the full connection string in the settings.
`{server_name}` can be found in the Azure SQL Database > Overview.
`{db_name}` can be found in Azure SQL Database.
In order to test the application locally, you need to set `ConnectionOption` to `RunAs=Developer; DeveloperTool=VisualStudio`.
`{tenant_id}` can be found in the subscription hosting your Azure SQL Database in Azure Active Directory > Overview.

`{appinsights_key}` can be found in **Azure Application Insights** > Overview > Instrumentation key.

In addition to these settings, there are several secrets that should be set to enable authentication with **Microsoft Graph Service** and **Recaptcha** validation if you are creating/approving users while developing locally. To setup:

1. ```cd .\Source\WebAPI```
2. ```dotnet user-secrets init```
3. ```dotnet user-secrets set ReCaptchaSecretKey "{recaptcha_secret_key}"```
4. ```dotnet user-secrets set AzureADB2B:ClientSecret "{your_aad_client_secret}"```

You can find the `{recaptcha_secret_key}` in the site you created at: https://www.google.com/recaptcha/admin.

The `{your_aad_client_secret}` can be found in Azure Active Directory > App registration > your App > Certificates and Secrets > Client Secrets. You may need to create one.

## Configuring Managed Identity access to Azure SQL Server database

Make yourself Azure Active Directory Admin on the Azure SQL Server. Go to the Query editor in the Azure SQL Database and run the following commands:

```
CREATE USER [<YOUR-OBJECT-ID>] FROM EXTERNAL PROVIDER
ALTER ROLE db_datareader ADD MEMBER [<YOUR-OBJECT-ID>] -- gives permission to read to database
ALTER ROLE db_datawriter ADD MEMBER [<YOUR-OBJECT-ID>] -- gives permission to write to database
```

To get your OID, use this command in e.g. Azure Cloud Shell: `az ad user show --id $MY_ACCOUNTNAME`.

 
$MY_ACCOUNTNAME is typically your email address, or if you are a guest user of this AAD Tenant, then it's {alias_company}#EXT#@{your_aad_account}.onmicrosoft.com.

Note: when you try to open Query editor, you may get an error stating that you cannot access the database from your IP address. Just add that IP to the firewall. The link to the firewall configuration is provided by the error message in Query editor. You will also need to do this when running the API with Visual Studio 2019 or otherwise the endpoints will fail with Http error 500 when trying to access the database from your IP address.

## Running and debugging the API locally with Visual Studio 2019

Open ```\Source\WebAPI.sln``` solution in Visual Studio 2019. Go to Tools > Options... > Azure Service Authentication > Account Selection and choose the account that will be used to access Azure Sql Database via Managed Identity. Then run ```WebAPI``` project. 

Your browser will open and navigate automatically to https://localhost:44325/swagger, where you can see the documentation of the different endpoints exposed by the API, and even try them from there if you provide the right JSON Web token (JWT) to Swagger. You could get a JWT with Postman. See [Postman section](#trying-the-api-with-postman) for details.

## Running Integration and Unit tests with Visual Studio 2019

Open ```\Source\WebAPI.sln``` solution and open Test Explorer (*View > Text Explorer* menu option or press *Ctr+E, T*). Then click *Run All Tests* icon or press *Ctrl+R, A*. You may run or debug specific set of tests or individual tests from that view.

## Trying the API with Postman
Details on how to configure authentication in Postman and how to try the different endpoints can be found in [Postman README](..\Postman\README.md).

## Getting a ReCaptcha token that the API can validate
Some endpoints might require a ReCatcha payload to validate that they are not being called by a bot. You may use the simple frontend found in [Source\ReCaptchaClient folder](\ReCaptchaClient) to generate that payload/token. Remember to change the ReCaptcha client key used `ReCaptchaClient\index.html` that corresponds to the `{recaptcha_secret_key}` used in the API.

## Deploying the API to Azure Web App
After deploying required infrastructure as explained in [main README, Getting Started section](..\README.md#Getting-Started), an Azure Web App where we can deploy the API will be available. Each app CI/CD pipeline defined in the [pipelines](..\Pipelines) folder builds, tests and deploys the API to the existing dev, staging and production environments.

All these pipelines rely on Azure DevOps Variable Groups to define the configuration for each environment. Those variable groups are ```dev-app-vg```, ```staging-app-vg``` and ```prod-app-vg``` respectively. The following variables overwrite the appsettings that are used during local development:

* appInsightsKey - key to send telemetry to app insights
* authScheme - AzureADB2B or AzureADB2C
* b2bAccountName - b2b domain name (without onmicrosoft.com)
* b2bClientId - App ID managing authentication with AAD
* b2bTenantId - Tenant where App ID is registered
* b2cAccountName - b2c account name
* b2cClientId - App ID managing authentication with AAD B2C
* b2cSignInPolicy - Scopes for B2C
* sqlConnectionString - SQL Database connection string
* webappUrl - publicly exposed web url
* keyVaultName - name of the Azure KeyVault

Additionally, there are two secrets that are managed via Azure KeyVault:

* B2bClientSecret - secret stored on AppId to access the Graph API
* reCAPTCHAServerKey - recaptcha key to authenticate client payloads
