# Publish CDM folder as REST endpoints

## Goal

The goal for this prototype is to study CDM SDK and see how it could be used to build REST endpoints over CDM data.

## Background

What are CDM and CDM folder?

The Common Data Model (CDM) is a standard and extensible collection of schemas (entities, attributes, relationships) that represents business concepts and activities with well-defined semantics, to facilitate data interoperability. Examples of entities include: Account, Contact, Lead, Opportunity, Product, etc.

More information about CDM can be found in <https://docs.microsoft.com/en-us/common-data-model/>

The CDM folder contains CDM metadata definition as model.json + data in CSV files, saved to Azure DataLake gen2 storage (<https://docs.microsoft.com/en-us/common-data-model/data-lake).>

CDM is already supported in the Common Data Service, Dynamics 365, PowerApps, Power BI, and number of Azure data services.

You can also extend CDM with your own data model, and save your data to CDM folder. You could have some legacy application like ERP or PLM that does not provide data in format that could be used from other applications like PowerBI or Azure ML. SDK supports writing data to CDM folder as well, but it's out of scope of this post.

## Sample data

It's easy to create sample data for demo purposes if your organization has Microsoft Dynamics and PowerApps. Note: only Admin can do this.

1. Create Azure DataLake gen 2 storage in Azure Portal
2. Sign-in to your tenant at <https://www.powerapps.com>
3. From left navigation select Data => Export to data lake (now in GA!)
4. Select DataLake gen 2 storage you created in first step
5. Select Common Data Service (CDS) Entities you want to export and accept changes (you can also select is export one-time only or continuous)

PowerApps exports selected Entities to DataLake:

We selected three Entities to be exported: systemuser, role and fax (because our CTO is a funny guy ;)).

CDM metadata file is called model.json and it is in root folder. Entity specific data is saved to subfolders "role" and "systemuser". Strangely enough we didn't have any fax messages in Dynamics so there is no "fax" subfolder.

If you open model.json file you can find the Entity definitions and related data partitions. Entity definition contains number of attributes (name and datatype). 

## SDK

SDK and CDM metadata definitions can be found in <https://aka.ms/cdmrepo.> SDK can be used to both read and write data to/from CDM folder. 

Please note SDK is quite new and there are still some issues with it. I had one problem with it, in model.json partition uri contains https port number, but ADSL storage adapter didn't support it. Luckily SDK contains source code so I could fix it by myself.

## Proto API implementation

### Warning

The implementation is quick-and-dirty sample how CDM folder can be provided as REST APIs. Please don't use it as-is in production. There are lots of things need to be implemented/improved before using implementation in production like:

* Authentication
* Error handling
* Move secrects to KeyVault

### Implementation

I'm using Asp.Net core for API. First I created empty API with

 dotnet new web -n CDMApi

Then I copied SDK directory objectModel\CSharp\Microsoft.CommonDataModel.ObjectModel next to CDMApi directory, and added reference to it from CDMApi project.

There are one feature folder for each Entity (Features/Faxes, Features/Roles and Features/Systemusers). In addition there is shared folder for classes shared between Entities.

Feature folders contains three classes

* [EntityName]Controller - API controller
* [EntityName]Model - POCO
* [EntityName]Extensions - Extension method to configure DI for the feature

Shared folder contains

* ADLSSettings - DataLake settings
* CDMMetadataRepository - Read CDM folder and use EntityGenerator to read Entities into memory
* CDMQuery<T> - Provide generic method for querying in-memory entity data 
* CDMService<T> - Initialize and host Entity in-memory data
* CsvContentParser - CSV file parser
* EntityGenerator - Build Entity object model and parse Entities by utilizing CsvContentParser

Note: Remember to set ADLS settings to appsettings.json file before running the application.

When application starts, it reads data from CDM folder into memory before it starts listening Http requests.

## Next steps

* It would be great to automatically create C# POCOs from CDM
* Support for additional Entity metadata like primaryKey
* Support for Entity relationships
* Support for all datatypes CDM supports
* Support all CSV formats CDM supports (or even better take one of well tested CSV parsers into use)
* Re-read data when it changes
* How to handle big datasets?
* Improve error handling
* Write your own metadata and data to CDM folder
