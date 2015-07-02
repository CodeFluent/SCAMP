# Simple Cloud Manager Project #
This repository contains all the executable code associated with the Simple Cloud Manager Project (SCAMP). SCAMP allows for the simplified management of Azure hosted virtual machines and web sites by providing an easy to use user interface and basic cost control measures. 

For more details, please see [www.simplecloudmgr.org](http://www.simplecloudmgr.org)

##Pre-requisites##
SCAMP has several dependencies that must be met. These are as follows:

**Visual Studio 2015 (min version: RC Community Edition)** - SCAMP has been built based on ASP.NET 5 (DNX). As a result you need this version of Visual Studio to work with the code. SCAMP is not currently compatible with Visual Studio Core due to several nuget package dependencies that are not yet compatible with DNX. 

**Azure Hosted Services** - SCAMP, as a cloud management solution also has dependencies on the following Azure hosted services:

- Azure Storage (tables)
- Document DB
- An Azure Web app slot if you want to host SCAMP for others.
- Azure Active Directory (ability to register an application and access keys)
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
The next step in setting up SCAMP is to set up the necessary hosted services on which SCAMP will depend. This section will walk you through creating and configuring these services. This section is based on your familiarity with Azure portals, [new portal](https://portal.azure.com) or [old portal](https://manage.windowsazure.com), you will be required to switch between the two to setup some services.

As you set up the resources, please pay close attention to the values you are asked to capture. These will be used later when you try to run SCAMP for the first time. Additionally, when possible you may want to place all the Azure services into the same resource group. When not possibly, you'll at least want to place them into the same Azure region.  

NOTE: If you are working as part of a team, you may opt to have a single set of services that are shared by all the team members. If this is the case, you may be provided with all the necessary application configuration values and can proceed to the [Running SCAMP](#running-scamp-for-the-first-time) section of this document.

### Create an Azure Storage account
SCAMP requires 1-3 storage accounts. For most implementations, a single storage account will do, but the system was built to support separate accounts should you need additional scalability from your solution.

Following [the official instructions to create a storage account](https://azure.microsoft.com/en-us/documentation/articles/storage-create-storage-account/) in azure. When you have finished creating the account, you will need to use the Account Name and one of its keys to [create an **Azure Storage Connection String**](https://azure.microsoft.com/en-us/documentation/articles/storage-configure-connection-string/) to be used when we set up the [run-time configuration](#running-scamp-for-the-first-time) for SCAMP later in this document.The string will follow the following format:

	DefaultEndpointsProtocol=[http|https];AccountName=myAccountName;AccountKey=myAccountKey

### Create a DocumentDB collection
SCAMP requires a single DocumentDB account,switch to the [new Azure portal](https://portal.azure.com). Please [leverage the official instructions  to create an account](https://azure.microsoft.com/en-us/documentation/articles/documentdb-create-account/). For development as well as many small SCAMP deployments, the *S1* account tier will be acceptable. The DocumentDB account creation process could take upwards of 10 minutes. 

After you have created the account, be sure to note the **account URI**, and one of its **secret keys**. These can be found by viewing the settings->keys blade in the Azure portal. 

Next up, you'll need to [create a database](https://azure.microsoft.com/en-us/documentation/articles/documentdb-create-database/), and [a collection](https://azure.microsoft.com/en-us/documentation/articles/documentdb-create-collection/). Note the name of the database and the collection as these will be used to set up the [run-time configuration](#running-scamp-for-the-first-time) for SCAMP later in this document

### Create an Azure Web App
If you intend to make SCAMP available to others, you will need to create an Azure Web App to host the solution so others can access it. A basic tutorial on provisioning a Web App can be [found here](https://azure.microsoft.com/en-us/documentation/articles/app-service-web-app-azure-portal/#navigation-example-create-a-web-app). A Basic (*B1*) plan will be sufficient for most small implementations. If the slot is only to be used for development purposes, the Free or Shared tiers may be preferable.

When setting up the site, please note the **Web App URL** that will be used to access it. This value will be used in the next step. 

If prompted while creating the site, do not select from any of the available gallery options. In this step, we're creating an empty deployment slot that we will deploy SCAMP to later. 

### Registering your application with Azure AD ###

SCAMP uses Azure Active Directory to authenticate users. You can use an existing Azure Active Directory or Office 365 tenant (all Azure subscriptions have one associated with it). Or create a new AAD tenant in the [Portal](https://manage.windowsazure.com).  See [http://www.windowsazure.com](http://www.windowsazure.com).  

#### Step 1: Register your application with Azure Active Directory
To allow SCAMP to use your tenant, you will need to register the application URLs that could be used. When running locally, the SCAMP project defaults to HTTP://locahost:44000. Alternatively, you can access it from HTTPS://localhost:44300. If you plan to run it from a host Azure Web App, you'll need the URL you captured from the previous step. 

To register your application, follow the appropriate instructions:
- [Register with Azure AD](https://azure.microsoft.com/en-us/documentation/articles/mobile-services-how-to-register-active-directory-authentication/)
- [Register with Office 365](https://msdn.microsoft.com/en-us/office/office365/howto/add-common-consent-manually)

If you have not registered an App with Azure or familiar with Azure portal, “Register with office 365” link covers the steps on how to navigate Azure management portal, to register your app with Active Directory, find out your Client ID, and App Secret. Which you will need in later sections.

In both cases, make sure you register the URL for each location you want to be able to run the application from. You'll also need to note your tenant ID (mydomain.com), the tenant ID (available from the portal URL or via [PowerShell](http://blogs.technet.com/b/heyscriptingguy/archive/2013/12/31/get-windows-azure-active-directory-tenant-id-in-windows-powershell.aspx)), and the client ID for each application you registered. 
To get your tenant ID from Azure portal URL, you will have to be on the [old Azure portal](https://manage.windowsazure.com). 
1.	Navigate to your Active Directory in the left navigation panel on management portal.
2.	Click on name of your Active Directory.
3.	On the page of your  Active Directory, take note of the guide in the URL section /Directory/ [Tenant ID] /directoryQuickStart 


#### Step 2:  Enable the OAuth2 implicit grant for your application

By default, applications provisioned in Azure AD are not enabled to use the OAuth2 implicit grant. In order to run this sample, you need to explicitly opt in.

1. From the former steps, your browser should still be on the Azure management portal - and specifically, displaying the Configure tab of your application's entry.
2. Using the Manage Manifest button in the drawer, download the manifest file for the application and save it to disk.
3. Open the manifest file with a text editor. Search for the `oauth2AllowImplicitFlow` property. You will find that it is set to `false`; change it to `true` and save the file.
4. Using the Manage Manifest button, upload the updated manifest file. Save the configuration of the app.

#### Step 3: create an identity
If you don't already have an identity in this Azure AD tenant, create one now. This identity will be required to access the application. 
 
### Create an Azure KeyVault repository
[Follow the instructions for provisioning an Azure KeyVault](https://azure.microsoft.com/en-us/documentation/articles/key-vault-get-started/). For this step you will need Azure PowerShell version 0.8.13 or later.

While executing those instructions, you can reuse one of the applications you registered earlier or create a new one. You can also skip the steps on setting up a hardware security model. 

Just be certain that during the setup you save the **Client Id** of the registered Azure AD Application that contains the key you configured for KeyVault. You'll also need to save the associated **key value** (as once its created it can never be seen again).For SCAMP, we'll refer to this value as the **AuthClientSecret**. 

The other deviation is that for SCAMP to interact with the KeyVault, you'll need to replace the permissions you'll need to grant the application will need to be a bit different then those provided in the write-up. 

	set-azurekeyvaultaccesspolicy -vaultname '{vault name here}' -serviceprincipalname '{client Id here}' -permissionstokeys all -permissionstosecrets all

**Note:** The permissions granted here should be considered temporary at this time. These will be more finely tuned before we finalize our release. 

It is possible to run SCAMP without a KeyVault, but then you won't be able to do any operations that require the access to the Azure subscription. This can be useful, for example, if one wants to develop the front-end and the user and group management functionalities, but doesn't want to create a KeyVault.
In this case, leave out all the configuration settings that start with `KeyVault:`.

##Running SCAMP for the first time##
Once you've configured the various services and captured the necessary configuration information, its time attempt to run the application locally. 

### Create the DocumentDB stored procedures
SCAMP's DocumentDB implementation is dependent on several stored procedures that must be created before the application can successfully run. The DocDBSetup project has been created solely for this purpose.
This console application accepts command line arguments. In Visual Studio, we can set these parameters and run the application by following these steps. 

1. Right-click on the DocDBSetup project in Visual Studio's Solution Explorer
2. Select "Properties" from the pop-up menu
3. Select the "Debug" tab
4. In the "Command line arguments" text box, enter in the values for the Document DB endpoint URL, secret key, database name, and collection name se3perated by spaces
5. Right-click the DocDBSetup project in Visual Studio's Solution Explorer, and  select Debug -> Start new instance from the pop-up menu

This will run the console program using the parameters you provided. If the program encounters an error, check the values you provided to ensure they are correct. Continue until the program completes without error. 

### Set the configuration settings for the SCAMP Web Project
ASP.NET uses a launchSettings.json property file to provide configuration settings what would be environment variables when the solution is deployed to an Azure Web App. If you are working on a team and you have not been provided with such a file, you will need to create and populate it. 

1. Open the solution in Visual Studio 2015 RC
2. In the Projects -> ScampAPI -> Properties folder, create a file: **launchSettings.json**:
3. Open the file and place the following content into it

```javascript
{
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "http://localhost:44000/",
      "environmentVariables": {
                "APPSETTING_ClientId": "{clientId-from above App in AAD}",
                "APPSETTING_TenantId": "{tenantId-from above App in AAD}",
                "APPSETTING_RedirectUri": "https://localhost:44300/",
                "APPSETTING_CacheLocation": "localStorage",
                "APPSETTING_DocDb:endpoint": "{ URL from https://portal.azure.com }",
                "APPSETTING_DocDb:databaseName": "{database name}",
                "APPSETTING_DocDb:collectionName": "{collection name}",
                "APPSETTING_DocDb:connectionMode" : "{http|tcp}",
                "APPSETTING_Provisioning:StorageConnectionString": "<storage connection string>",
				"APPSETTING_KeyVault:Url": "https://{name}.vault.azure.net/",
        		"APPSETTING_KeyVault:AuthClientId": "{clientId-from above App in AAD}",
        		"APPSETTING_KeyVault:AuthClientSecret": "{key associated with the above client ID}",
				"APPSETTING_ActivityLogStorage:ConnectionString": "{storage account for saving activity logs}",
				"APPSETTING_ResourceStateStorage:ConnectionString": "{storage account for saving resource states}",
				"APPSETTING_ActivityLogStorage:TableName": "{table name for Activity Logs}",
				"APPSETTING_ResourceStateStorage:TableName": "{table name for resource state storage}"

            }
        }
    }
}

````
The values used here are as follows:

- **TenantId** this is the Tenant ID of the AAD Domain. This can be retrieved from the Azure Portal from the URL.
- **ClientId** this is the Client ID for the Scamp application once it's been setup in an AAD tenant. This comes from the Applications Configure page for that specific AAD Tenant.
- **CacheLocation** this is a setting that ADAL uses on where 'session' will be managed.
- **DocDB:endpoint** this is the DocumentDB URL that comes from the [Azure Preview Portal](https://portal.azure.com)
- **DocDB:authkey** this is the DocumentDB key that comes from the [Azure Preview Portal](https://portal.azure.com)
- **DocDb:databaseName** the name of the document database you created
- **DocDb:collectionName** the name of the document collection you created
- **DocDb:connectionMode** specify http for HTTP Mode, or tcp for direct connection to DocumentDB. tcp is recommended if firewalls/proxies allow it. http is likely simplest for local dev
- **Provisioning:StorageConnectionString** this is an Azure Storage Account connection string in the format of:
- **KeyVault:Url** this is the full url of the KeyVault repository (eg.https://scampkeyvault.vault.azure.net/)
- **KeyVault:AuthClientId** this is the client id of the Azure AD that is accessing keyvault
- **KeyVault:AuthClientSecret** this is the secret of the Azure AD app that is accessing keyvault

With the configuration settings in place, you can launch the project. 
1. Right-click on the ScampApi project in Visual Studio's solution explorer
2. select "Set as StartUp Project' from the pop-up menu
3. Verify that in the tool bar, that "IIS Express" and "Debug" are selected
4. optionally: select the down arrow to the right of "IIS Express" and set the Web Browser to the browser of your choice. 
5. Press F5 or the green arrow to the left of "IIS Express" to launch the application. 

Unless a build error occurs, you shoud see a browser window appear and after a few seconds the SCAMP login screen wil appear. You can now test the application by logging in using an identity that exists in the Azure AD tenant you configured the application to use. 

After successfully logging in, you wil be presented with the SCAMP "user dashboard" view. The web user interface is now ready for use. 

### Set the configuration settings for the Web Jobs
SCAMP also includes two web jobs that will need to be run for full functionality of the application to be available. 

TODO: describe setting these up

