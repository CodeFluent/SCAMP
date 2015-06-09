# Simple Cloud Manager Project #
This repository contains all the executable code associated with the Simple Cloud Manager Project (SCAMP). SCAMP allows for the simplified management of Azure hosted virtual machines and web sites by providing an easy to use user interface and basic cost control measures. 

For more details, please see [www.simplecloudmgr.org](http://www.simplecloudmgr.org)

##Pre-requisites##
SCAMP has several dependencies that must be met. These are as follows:

**Visual Studio 2015 (min version: RC Community Edition)** - SCAMP has been built based on ASP.NET 5 (DNX). As a result you need this version of Visual Studio to work with the code. SCAMP is not currently compatible with Visual Studio Core due to several nuget package dependencies that are not yet compatible with DNX. 

**Azure Hosted Services** - SCAMP, as a cloud management solution also has dependencies on the following Azure hosted services:


- Azure Active Directory (ability to register an application and access keys)
- Document DB
- Azure Storage (tables)
- Key Vault
- Azure Subscription Access: a user identity with permissions to create/manage Virtual Machines and Web Sites.

Instructions for helping provision these services and configuring SCAMP to leverage them can be found later in this document. 

##First Time Build##
When starting work with SCAMP, we encourage you to attempt to clone the source code and get a "clean" build. SCAMP has many Nuget package dependencies and this helps ensure that they are all resolved cleanly. 

#### Step 1:  Clone or download this repository

From your shell or command line:

	git clone https://github.com/SimpleCloudManagerProject/Scamp

#### Step 2: Open Project
Launch Visual Studio 2015 and from the file menu, select File->Open->Project/Solution. Navigate to the folder/directory where you cloned the SCAMP repository and select the file *Scamp.sln*. 

Visual Studio will begin to load the project. This also involves the download of all dependent Nuget packages. Depending on the speed of your internet connection, this process could take several minutes. You can monitor the process via the "output" window. 

Wait for the process to complete and the project to be fully loaded before continuing to the next step. 

#### Step 3: Build the solution
With the Nuget packages all resolved, we're ready to build the project. In Visual Studio select Build->Rebuild Solution to start the process. Due to some yet unknown issues, your first build attempt may fail. If it does, simply try the build again and it should complete without error.

If you encounter an error at any point in this process, [please post an issue](https://github.com/SimpleCloudManagerProject/SCAMP/issues/new) to our GitHub repository.  

##Azure Services Setup##
The next step in setting up SCAMP is to set up the necessary hosted services on which SCAMP will depend. In this section will walk you through creating and configuring these services.

As you set up the resources, please pay close attention to the values you are asked to capture. These will be used later when you try to run SCAMP for the first time. Additionally, when possible you may want to place all the Azure services into the same resource group. When not possibly, you'll at least want to place them into the same Azure region.  

NOTE: If you are working as part of a team, you may opt to have a single set of services that are shared by all the team members. If this is the case, you may be provided with all the necessary application configuration values and can proceed to the [Running SCAMP](#Running SCAMP for the first time) section of this document.

### Create an Azure Storage account
SCAMP requires 1-3 storage accounts. For most implementations, a single storage account will do, but the system was built to support separate accounts should you need additional scalability from your solution.

Following [the official instructions to create a storage account](https://azure.microsoft.com/en-us/documentation/articles/storage-create-storage-account/) in azure. When you have finished creating the account, you will need to save the **Azure Storage Connection String** for the account to be used when we set up the runtime configuration for SCAMP later in this document. [This article](https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/) explains how to use the storage account name and key to craft a connection string like the following

	DefaultEndpointsProtocol=[http|https];AccountName=myAccountName;AccountKey=myAccountKey

### Create a DocumentDB collection
SCAMP requires a single DocumentDB account. Please [leverage the official instructions  to create an account](https://azure.microsoft.com/en-us/documentation/articles/documentdb-create-account/). For development as well as many small SCAMP deployments, the *S1* account tier will be acceptable. The DocumentDB account creation process could take upwards of 10 minutes. 

After you have created the account, be sure to note the **account URI**, and one of its **keys**. These can be found by viewing the settings->keys blade in the Azure portal. 

Next up, you'll need to [create a database](https://azure.microsoft.com/en-us/documentation/articles/documentdb-create-database/), and [a collection](https://azure.microsoft.com/en-us/documentation/articles/documentdb-create-collection/). Note the name of the database and the collection as these will be used to set up the runtime configuration for SCAMP later in this document

### Setting Azure Active Directory Tenant ###

You an use an existing Azure Active Directory AAD tenant (all Azure subscriptions have one associated with it). Or create a new AAD tenant in the [Portal](https://manage.windowsazure.com).  See [http://www.windowsazure.com](http://www.windowsazure.com).  

#### Step 1:  Register the Scamp Web App on your Azure Active Directory tenant

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "Scamp", select "Web Application and/or Web API", and click next.
8. For the sign-on URL, enter the base URL for the sample, `https://localhost:44300/`. We'll use this later.
9. For the App ID URI, enter `https://<your_tenant_name>/Scamp`, replacing `<your_tenant_name>` with the name of your Azure AD tenant.  Save the configuration.
10. Also, get the TenantID from the URL in your browser. eg. below in the URL you'll see a guid which is your Tenant ID.
```
../ActiveDirectoryExtension/Directory/<tenantId>/directoryQuickStart
```
11. Also, get the ClientID from the Application settings.  On the Application -> Configure tab, grap the "CLIENT ID" value.

Repeat steps 5-11 for a non HTTP URL such as 'http://localhost:44000/' if you wish to do development without SSL. 

#### Step 2:  Creating an application key
This will be used by Key Vault, allowing the application access to the vault's secrets. 

TODO

#### Step 3:  Enable the OAuth2 implicit grant for your application

By default, applications provisioned in Azure AD are not enabled to use the OAuth2 implicit grant. In order to run this sample, you need to explicitly opt in.

1. From the former steps, your browser should still be on the Azure management portal - and specifically, displaying the Configure tab of your application's entry.
2. Using the Manage Manifest button in the drawer, download the manifest file for the application and save it to disk.
3. Open the manifest file with a text editor. Search for the `oauth2AllowImplicitFlow` property. You will find that it is set to `false`; change it to `true` and save the file.
4. Using the Manage Manifest button, upload the updated manifest file. Save the configuration of the app.


### Create a KeyVault repository
For this step you will need Azure PowerShell version 0.8.13 or later.
You can also read the following tutorials to get familiar with Azure Resource Manager in Windows PowerShell:

- [How to install and configure Azure PowerShell](powershell-install-configure.md)
- [Using Windows PowerShell with Resource Manager](powershell-azure-resource-manager.md)

Start an Azure PowerShell session and sign in to your Azure account with the following command:  

    Add-AzureAccount

In the pop-up browser window, enter your Azure account user name and password. Windows PowerShell will get all the subscriptions that are associated with this account and by default, uses the first one.

If you have multiple subscriptions and want to specify a specific one to use for Azure Key Vault, type the following to see the subscriptions for your account:

    Get-AzureSubscription

Then, to specify the subscription to use, type:

    Select-AzureSubscription -SubscriptionName <subscription name>

If you haven't already done so, [download the scripts](https://gallery.technet.microsoft.com/scriptcenter/Azure-Key-Vault-Powershell-1349b091) and unblock the "Azure Key Vault Powershell scripts.zip" file by right-clicking it, **Properties**, **Unblock**. Then extract the zip file to a local folder on your computer.

Before you load the script module into your Azure PowerShell session, set the execution policy:

			Set-ExecutionPolicy RemoteSigned -Scope Process

Then load the script module into your Azure PowerShell session. For example, if you extracted the scripts to a folder named C:\KeyVaultScripts, type:

			import-module C:\KeyVaultScripts\KeyVaultManager

The Key Vault cmdlets and scripts require Azure Resource Manager, so type the following to switch to Azure Resource Manager mode:

	Switch-AzureMode AzureResourceManager

	New-AzureKeyVault -VaultName 'ScampKeyVault' -ResourceGroupName 'ScampResourceGroup' -Location 'North Europe'

- **VaultName** will be the **KeyVault:Url** that you will use later in the launchSettings.json file.
- ResourceGroupName is the Resource Group Name in Azure.
- Location parameter, use the command [Get-AzureLocation](https://msdn.microsoft.com/library/azure/dn654582.aspx). If you need more information, type: `Get-Help Get-AzureLocation`

Applications that use a key vault must authenticate and has permission granted.

1. Sign in to the Azure Management Portal.
2. On the left, click **Active Directory**, and then select the directory you have used previously.
3. On the Quick Start page, click Applications then your app and finally click **CONFIGURE**.
4. Scroll to the **keys** section, select the duration, and then click **SAVE**. The page refreshes and now shows a key value. This value will be **KeyVault:AuthClientSecret** in the config.
5. Copy the client ID value from this page. This value will be  **KeyVault:AuthClientId** in the config.

##Running SCAMP for the first time##


### Step 6:  Configure the Scamp Application to use your Azure Active Directory tenant

1. TODO:
2. Open the solution in Visual Studio 2015 RC
3. In the Projects -> ScampAPI -> Properties folder, create a file: **launchSettings.json**:


```javascript
{
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "http://localhost:44000/",
      "environmentVariables": {
                "APPSETTING_ClientId": "<clientId-from above App in AAD>",
                "APPSETTING_TenantId": "<tenantId-from above App in AAD->",
                "APPSETTING_RedirectUri": "https://localhost:44300/",
                "APPSETTING_CacheLocation": "localStorage",
                "APPSETTING_DocDb:endpoint": "< URL from https://portal.azure.com >",
                "APPSETTING_DocDb:databaseName": "scamp",
                "APPSETTING_DocDb:collectionName": "scampdata",
                "APPSETTING_DocDb:connectionMode" : "http|tcp",
                "APPSETTING_Provisioning:StorageConnectionString": "<storage connection string>",
				"APPSETTING_KeyVault:Url": "https://{name}.vault.azure.net/",
        		"APPSETTING_KeyVault:AuthClientId": "{Active Directory Client ID}",
        		"APPSETTING_KeyVault:AuthClientSecret": "{Active directory secret}",
				"APPSETTING_ActivityLogStorage:ConnectionString": "<storage account for saving activity logs>",
				"APPSETTING_ResourceStateStorage:ConnectionString": "<storage account for saving resource states>",
				"APPSETTING_ActivityLogStorage:TableName": "<table name for Activity Logs>",
				"APPSETTING_ResourceStateStorage:TableName": "<table name for resource state storage>"

            }
        }
    }
}

````

### Running with specific Tenant and Client ID ###

This makes use of environment variables that need to be added.

In your Package Manager Console, before you debug - add $env variabiels.

    PM> $env:APPSETTING_TenantId = "foo"
    PM> $env:APPSETTING_ClientId = "bar"
    PM> $env:APPSETTING_CacheLocation": "localStorage"
    PM> $env:APPSETTING_DocDb:endpoint = "<url here>"
    PM> $env:APPSETTING_DocDb:authkey = "<key here>"
    PM> $env:APPSETTING_DocDb:databaseName = "<db name here, e.g. scamp>"
    PM> $env:APPSETTING_DocDb:collectionName = "<collection name>"
    PM> $env:APPSETTING_DocDb:connectionMode = "http|tcp"
    PM> $env:APPSETTING_Provisioning:StorageConnectionString = "<azure storage account connection string>"
		PM> $env:APPSETTING_KeyVault:Url = "https://{name}.vault.azure.net/"
		PM> $env:APPSETTING_KeyVault:AuthClientId = "{Active Directory Client ID}"
		PM> $env:APPSETTING_KeyVault:AuthClientSecret = "{Active directory secret}"

Or, these can be set also from Project Properties -> Debug -> Environment Variables to set.
This format is used as this is what AZW uses for Environment variables.


````
APPSETTING_TenantId
APPSETTING_ClientId
APPSETTING_CacheLocation
APPSETTING_DocDb:endpoint
APPSETTING_DocDb:authkey
APPSETTING_DocDb:databaseName
APPSETTING_DocDb:collectionName
APPSETTING_DocDb:connectionMode
APPSETTING_Provisioning:StorageConnectionString
APPSETTING_KeyVault:Url
APPSETTING_KeyVault:AuthClientId
APPSETTING_KeyVault:AuthClientSecret
````

### Settings For Site ###
- **TenantId** this is the Tenant ID of the AAD Domain. This can be retrieved from the Azure Portal from the URL.
- **ClientId** this is the Client ID for the Scamp application once it's been setup in an AAD tenant. This comes from the Applications Configure page for that specific AAD Tenant.
- **CacheLocation** this is a setting that ADAL uses on where 'session' will be managed.
- **DocDB:endpoint** this is the DocumentDB URL that comes from the [Azure Preview Portal](https://portal.azure.com)
- **DocDB:authkey** this is the DocumentDB key that comes from the [Azure Preview Portal](https://portal.azure.com)
- **DocDb:databaseName** this is '**scamp**' by default the Scamp code will create this if it doesn't exist already
- **DocDb:collectionName** this is '**scampdata**' by default the Scamp code will create this if it doesn't exist already.
- **DocDb:connectionMode** specify http for HTTP Mode, or tcp for direct connection to DocumentDB. tcp is recommended if firewalls/proxies allow it. http is likely simplest for local dev
- **Provisioning:StorageConnectionString** this is an Azure Storage Account connection string in the format of:
- **KeyVault:Url** this is the full url of the KeyVault repository (eg.https://scampkeyvault.vault.azure.net/)
- **KeyVault:AuthClientId** this is the client id of the Azure AD that is accessing keyvault
- **KeyVault:AuthClientSecret** this is the secret of the Azure AD app that is accessing keyvault
```
"DefaultEndpointsProtocol=https;AccountName=[AccountName];AccountKey=[AccountKey]"
```
