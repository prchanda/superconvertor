using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace SuperConvertor.Controllers
{
    public class HomeController : Controller
    {
        private static CloudBlobContainer inputDocumentsContainer, outputDocumentsContainer;
        private static QueueClient IncomingQueueClient, OutgoingClient;

        public HomeController()
        {
            InitializeStorage();
        }

        public string ConversionType { get; set; }
        public static HashSet<string> OutputBlobUrls { get; set; }

        private void InitializeStorage()
        {
            // Open storage account using credentials from .cscfg file.
            var storageAccount =
                CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var serviceBusConnectionString =
                RoleEnvironment.GetConfigurationSettingValue("Microsoft.ServiceBus.ConnectionString");

            // Get context object for working with blobs, and 
            // set a default retry policy appropriate for a web user interface.
            var blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            // Get a reference to the blob container.
            inputDocumentsContainer = blobClient.GetContainerReference("inputdocuments");
            outputDocumentsContainer = blobClient.GetContainerReference("outputdocuments");

            IncomingQueueClient =
                QueueClient.CreateFromConnectionString(serviceBusConnectionString, "conversionrequest");
            OutgoingClient = QueueClient.CreateFromConnectionString(serviceBusConnectionString, "processedmessage");

            var blobs = outputDocumentsContainer.ListBlobs(useFlatBlobListing: true);
            OutputBlobUrls = new HashSet<string>();
            foreach (var blob in blobs)
                OutputBlobUrls.Add(blob.StorageUri.PrimaryUri.ToString());
        }

        [OutputCache(NoStore = true, Location = OutputCacheLocation.Client, Duration = 2)]
        public ActionResult GetConvertedFiles()
        {
            if (OutgoingClient.Peek() != null)
            {
                var message = OutgoingClient.Receive();
                OutputBlobUrls.Add(message.GetBody<string>(new DataContractSerializer(typeof(string))));
                message.Complete();
            }

            return PartialView("_ConvertedFiles", OutputBlobUrls);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(HttpPostedFileBase file, string ConversionType)
        {
            string _FileName = null;
            try
            {
                if (file.ContentLength > 0)
                {
                    _FileName = Path.GetFileName(file.FileName);
                    if (!_FileName.Contains(ConversionType.Split('-')[0]))
                    {
                        ViewBag.Message = "File extension is not matching with the chosen source conversion type!";
                    }
                    else
                    {
                        var fileBlob = await UploadAndSaveBlobAsync(file);
                        await IncomingQueueClient.SendAsync(new BrokeredMessage(
                            _FileName + "|" + ConversionType.Split('-')[1],
                            new DataContractSerializer(typeof(string))));
                        ViewBag.Message = "File Uploaded Successfully!!";
                    }
                }

                return View();
            }
            catch
            {
                ViewBag.Message = "File upload failed!!";
                return View();
            }
        }

        public string DownloadAndSaveBlob(string filename)
        {
            var fileBlob = outputDocumentsContainer.GetBlockBlobReference(filename);
            return fileBlob.Uri.AbsoluteUri;
        }

        private async Task<CloudBlockBlob> UploadAndSaveBlobAsync(HttpPostedFileBase file)
        {
            var blobName = Path.GetFileName(file.FileName);
            var fileBlob = inputDocumentsContainer.GetBlockBlobReference(blobName);
            using (var fileStream = file.InputStream)
            {
                await fileBlob.UploadFromStreamAsync(fileStream);
            }

            return fileBlob;
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}