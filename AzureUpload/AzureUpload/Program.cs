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
        static string downFolder;
        static string upFolder;
        static string upContainter;
        static string downContainer;

        static DateTime prevTime;
        static CloudStorageAccount storage_account;
        static CloudBlobClient blob_client;
        static CloudBlobContainer container_down;
        static CloudBlobContainer container_up;

        static void Main(string[] args)
        {
            connString = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            downFolder = ConfigurationManager.AppSettings["downFolder"];
            upFolder = ConfigurationManager.AppSettings["upFolder"];
            upContainter = ConfigurationManager.AppSettings["destContainer"];
            downContainer = ConfigurationManager.AppSettings["localContainer"];
            DateTime prevTime = new DateTime(2015, 3, 13); // Timestamp to start analyze


            Console.Write("connect to the account \n");
            storage_account = CloudStorageAccount.Parse(connString);
            blob_client = storage_account.CreateCloudBlobClient();

            Console.Write("get reference to container \n");
            container_down = blob_client.GetContainerReference(downContainer);
            container_up = blob_client.GetContainerReference(upContainter);


            container_down.CreateIfNotExists();
            container_up.CreateIfNotExists();

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
                foreach (var blockblob in container_down.ListBlobs())
                {
                    CloudBlockBlob b = (CloudBlockBlob)blockblob;
                    string path = downFolder + "\\" + b.Name;
                    DownloadBlob(b, path, prevTime);
                }
                prevTime = DateTime.Now;
            }
        }

        static void uploadFile()
        {
            while (true)
            {
                Thread.Sleep(5000);
                string[] fileEntries = Directory.GetFiles(upFolder);
                foreach (string filePath in fileEntries)
                {
                    //string key = DateTime.UtcNow.ToString("yyyy-MM-dd-HH") + "-" + Path.GetFileName(filePath);
                    UploadBlob(container_up, filePath, Path.GetFileName(filePath), prevTime);

                }
                prevTime = DateTime.Now;
            }
        }

        static void UploadBlob(CloudBlobContainer container, string filePath, string filename, DateTime dt)
        {
            CloudBlockBlob b = container.GetBlockBlobReference(filename);

            using (var fs = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                BlobRequestOptions options = new BlobRequestOptions();
                AccessCondition ac = AccessCondition.GenerateIfModifiedSinceCondition(dt.ToUniversalTime());

                try
                {
                    b.UploadFromStream(fs, ac, null, null);
                    Console.WriteLine(@"uploading file to container: key =" + filename);
                }
                catch (StorageException)
                {
                    Console.Write("no file to upload\n");
                }
            }

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
