using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SuperConvertor
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            //Trace.TraceInformation("Creating InputDocuments blob container");
            var blobClient = storageAccount.CreateCloudBlobClient();
            var inputBlobContainer = blobClient.GetContainerReference("inputdocuments");
            if (inputBlobContainer.CreateIfNotExists())
                inputBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });

            var outputBlobContainer = blobClient.GetContainerReference("outputdocuments");
            if (outputBlobContainer.CreateIfNotExists())
                outputBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });

            // Thread-safe. Recommended that you cache rather than recreating it
            // on every request.            
            // Obtain these values from the portal.
            var Namespace = "pratyay";

            var uri = ServiceBusEnvironment.CreateServiceUri("sb", Namespace, string.Empty);
            var tP = TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey",
                "zT1+iZ/5mfikMpLrZXB+FbPelZ6GzqUqCBVbtVc3wtw=");

            ServiceBusEnvironment.SystemConnectivity.Mode = ConnectivityMode.Http;
            // Create the namespace manager which gives you access to
            // management operations.
            var namespaceManager = new NamespaceManager(uri, tP);

            // Create the queue if it does not exist already.

            if (!namespaceManager.QueueExists("conversionrequest")) namespaceManager.CreateQueue("conversionrequest");

            if (!namespaceManager.QueueExists("processedmessage")) namespaceManager.CreateQueue("processedmessage");
            //Trace.TraceInformation("Storage initialized");
        }
    }
}