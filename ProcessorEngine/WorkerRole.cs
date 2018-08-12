using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ProcessorEngine
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue        
        private static CloudBlobContainer outputDocumentsContainer;
        private static CloudBlobContainer inputDocumentsContainer;

        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient IncomingClient, OutgoingClient;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            //Trace.WriteLine("Starting processing of messages");            
            // Initiates the message pump and callback is invoked for each message that is received, calling close on the client will stop the pump.
            IncomingClient.OnMessage((receivedMessage) =>
                {
                    try
                    {
                        // Process the message                       
                        string body = receivedMessage.GetBody<string>(new DataContractSerializer(typeof(string)));
                        receivedMessage.Complete();
                        receivedMessage.Abandon();

                        CloudBlockBlob fileBlob = inputDocumentsContainer.GetBlockBlobReference(body.Split('|')[0]);
                        LocalResource localResource = RoleEnvironment.GetLocalResource("FileStorage");                        
                        fileBlob.DownloadToFile(Path.Combine(localResource.RootPath, body.Split('|')[0]), FileMode.Create);
                        string executable = null;                        

                        if (body.Split('|')[0].Contains("doc"))
                            executable = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\docto.exe");
                        else
                            executable = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\XlsTo.exe");                        

                        switch (body.Split('|')[1])
                        {
                            case "pdf":
                            ExecuteFileProcessing(executable, body.Split('|')[0], localResource.RootPath, "pdf|wdFormatPDF");
                                break;
                            case "txt":
                                ExecuteFileProcessing(executable, body.Split('|')[0], localResource.RootPath, "txt|xlTextWindows");
                                break;
                            case "rtf":
                                ExecuteFileProcessing(executable, body.Split('|')[0], localResource.RootPath, "rtf|wdFormatRTF");
                                break;
                            case "csv":
                                ExecuteFileProcessing(executable, body.Split('|')[0], localResource.RootPath, "csv|xlCSV");
                                break;
                            case "html":
                                ExecuteFileProcessing(executable, body.Split('|')[0], localResource.RootPath, "html|" + (executable.Contains("doc") ? "wdFormatHTML" : "xlHtml"));
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                    }
                });

            CompletedEvent.WaitOne();            
        }

        private void ExecuteFileProcessing(string exePath, string inputFileName, string outputPath, string format)
        {
            Process fileConvertor = new Process();

            try
            {                
                ProcessStartInfo startInfo = new ProcessStartInfo(exePath);
                startInfo.Arguments = "-f \"" + Path.Combine(outputPath, inputFileName) + "\"" + " -O " + "\"" +
                    Path.Combine(outputPath, inputFileName.Split('.')[0] + "." + format.Split('|')[0]) + "\"" + " -T " + format.Split('|')[1] + " -L 10 -G";
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.ErrorDialog = true;                

                fileConvertor = Process.Start(startInfo);
                fileConvertor.WaitForExit();
                                
                CloudBlockBlob fileBlob = outputDocumentsContainer.GetBlockBlobReference(inputFileName.Split('.')[0] + "." + format.Split('|')[0]);                
                fileBlob.UploadFromFile(Path.Combine(outputPath, inputFileName.Split('.')[0] + "." + format.Split('|')[0]));                
                OutgoingClient.Send(new BrokeredMessage(inputFileName.Split('.')[0] + "." + format.Split('|')[0], new DataContractSerializer(typeof(string))));                
                File.Delete(Path.Combine(outputPath, inputFileName));
                File.Delete(Path.Combine(outputPath, inputFileName.Split('.')[0] + "." + format.Split('|')[0]));
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");      

            //// Initialize the connection to Service Bus Queue
            IncomingClient = QueueClient.CreateFromConnectionString(connectionString, "conversionrequest");
            OutgoingClient = QueueClient.CreateFromConnectionString(connectionString, "processedmessage");

            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConectionString"));

            var blobClient = storageAccount.CreateCloudBlobClient();
            outputDocumentsContainer = blobClient.GetContainerReference("outputdocuments");
            inputDocumentsContainer = blobClient.GetContainerReference("inputdocuments");
            return base.OnStart();
        }

        public void LogError(Exception exp)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";
                eventLog.WriteEntry(exp.Message + Environment.NewLine + exp.StackTrace, EventLogEntryType.Error, 101, 1);
            }
        }

        public override void OnStop()
        {            
            // Close the connection to Service Bus Queue
            IncomingClient.Close();
            OutgoingClient.Close();
            CompletedEvent.Set();
            base.OnStop();
        }
    }
}
