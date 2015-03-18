using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace AzureUpload
{
    class Program
    {
        static string connString;
        static string localFolder;
        static string destContainter;
        static DateTime prevTime;
        static CloudStorageAccount storage_account;
        static CloudBlobClient blob_client;
        static CloudBlobContainer container;

        static void Main(string[] args)
        {
            connString = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            localFolder = ConfigurationManager.AppSettings["sourceFolder"];
            destContainter = ConfigurationManager.AppSettings["destContainer"];
            DateTime prevTime = new DateTime(2015, 3, 13); // Timestamp to start analyze


            Console.Write("connect to the account \n");
            storage_account = CloudStorageAccount.Parse(connString);
            blob_client = storage_account.CreateCloudBlobClient();

            Console.Write("get reference to container \n");
            container = blob_client.GetContainerReference(destContainter);

            container.CreateIfNotExists();

            System.Threading.Thread downloadThread = new System.Threading.Thread(downloadFile);
            System.Threading.Thread uploadThread = new System.Threading.Thread(uploadFile);
            downloadThread.Start();
            uploadThread.Start();
        }

        static void downloadFile()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                foreach (var blockblob in container.ListBlobs())
                {
                    CloudBlockBlob b = (CloudBlockBlob)blockblob;
                    string path = localFolder + "\\" + b.Name;
                    DownloadBlob(b, path, prevTime);
                }
                prevTime = DateTime.Now;
            }
        }

        static void uploadFile()
        {
        }

        static void UploadBlob(CloudBlobContainer container, string key, string filename, bool deleteAfter)
        {

            Console.WriteLine(@"uploading file to container: key =" + key + " sourcefile= " + filename);
            CloudBlockBlob b = container.GetBlockBlobReference(key);

            using (var fs = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                b.UploadFromStream(fs);
            }

            if (deleteAfter)
                File.Delete(filename);

        }

        static void DownloadBlob(CloudBlockBlob b, string path, DateTime currTime)
        {
            
            BlobRequestOptions option = new BlobRequestOptions();
            using (var fileStream = System.IO.File.OpenWrite(path))
            {
                try
                {
                    b.DownloadToStream(fileStream, AccessCondition.GenerateIfModifiedSinceCondition(currTime.ToLocalTime()));
                    Console.Write("Download to " + path + "\n");
                }
                catch(Microsoft.WindowsAzure.Storage.StorageException)
                {
                    Console.Write("No new file to download \n");
                }
            }
        }
    }
}
