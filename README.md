# Super Convertor

Super Convertor is a sample application that demonstrates the convertion of a Microsoft Word Document '.doc' file or an Excel Spreadsheet '.xls' file to any other supported file format such as .txt .csv .pdf. It uses two open source simple command line utilities [DocTo](https://github.com/tobya/DocTo/blob/master/readme.md) and [XLSTo](https://github.com/tobya/DocTo/blob/master/xlsTo.md) for converting .doc & .xls files to any supported format such as Text, RTF, or PDF. Refer [this](http://tobya.github.io/DocTo) for more information.

Please follow the below steps to setup the lab for [Cloud Service troubleshooting series](https://blogs.msdn.microsoft.com/pratyay/2018/07/30/cloud-service-troubleshooting-series/):

1.	Install Git client for windows. You can download the setup file from here : https://git-scm.com/download/win. Git clone the cloud service solution using the command : **git clone https://github.com/prchanda/superconvertor.git**.

2.	Create a classic storage account from Azure Portal. Refer [this](https://docs.microsoft.com/en-us/azure/storage/common/storage-create-storage-account#create-a-storage-account) article for guidance. If you already have a classic storage account you can skip this step.

3.	Create a service bus namespace from Azure Portal. Refer [this](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-create-namespace-portal) article for guidance. If you already have a service bus namespace you can skip this step.

4.  Open up the solution in Visual Studio and [configure the solution to use your Azure storage account when it runs in Azure](https://docs.microsoft.com/en-us/azure/cloud-services/cloud-services-dotnet-get-started#configure-the-solution-to-use-your-azure-storage-account-when-it-runs-in-azure) for *SuperConvertor* and *ProcessorEngine* role. To keep things simple set the storage account connection string setting name as **StorageConnectionString**, otherwise you need to update the references of the storage connection setting name in the application code.

5. Similarly like Step 4, configure your service bus connection string. To keep things simple set the service bus connection string setting name as **Microsoft.ServiceBus.ConnectionString**, otherwise you need to update the references of the service bus connection setting name in the application code.

6.  Publish the solution to Azure using the Visual Studio Publish Azure Application Wizard. You can refer [this](https://docs.microsoft.com/en-us/azure/vs-azure-tools-publish-azure-application-wizard) article if you are not aware as how to publish your cloud service solution to Azure. You dont have to wait for the whole deployment to get complete, you can proceed to the next step as soon as the cloud service roles are created.

7.  Now you should see a cloud service created under your azure subscription.
