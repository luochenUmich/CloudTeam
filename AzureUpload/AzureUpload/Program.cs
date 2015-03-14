using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureUpload
{
    class Program
    {
        static void Main(string[] args)
        {
            string connString = ConfigurationManager.ConnectionStrings["AzureStorageAccount"].ConnectionString;
            string localFolder = ConfigurationManager.AppSettings["sourceFolder"];
            string destContainter = ConfigurationManager.AppSettings["destContainer"];

            Console.Write(@"connect to the account");
            CloudStorageAccount sa = CloudStorageAccount.Parse(connString);
            CloudBlobClient bc = sa.CreateCloudBlobClient();

            Console.Write(@"get reference to con");
            CloudBlobContainer container = bc.GetContainerReference(destContainter);

            container.CreateIfNotExists();

            string[] fileEntries = Directory.GetFiles(localFolder);
            foreach (string filePath in fileEntries)
            {
                string key = DateTime.UtcNow.ToString("yyyy-MM-dd-HH:mm:ss") + "-" + Path.GetFileName(filePath);
                UploadBlob(container, key, filePath, false);
            }

            Console.WriteLine(@"upload processing complete");
            Console.ReadKey();

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
    }
}
