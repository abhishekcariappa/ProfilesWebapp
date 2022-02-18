using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using Microsoft.AspNetCore.Http;
using ProfileManager.Models;

namespace ProfileManager.Controllers
{
    public class StorageUploadController : Controller
    {
        private readonly IConfiguration _configuration;
        public StorageUploadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();           
           
        }

        [HttpPost]
        public async Task<IActionResult> UploadToBlob(IFormFile files)
        {
            string blobstorageconnection = _configuration.GetValue<string>("blobstorage");
            string containerName = _configuration.GetValue<string>("ContainerName");

            byte[] dataFiles;            
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);           
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();            
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };
            string systemFileName = files.FileName;
            await cloudBlobContainer.SetPermissionsAsync(permissions);
            await using (var target = new MemoryStream())
            {
                files.CopyTo(target);
                dataFiles = target.ToArray();
            }
            
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(Guid.NewGuid().ToString()+ systemFileName);
            await cloudBlockBlob.UploadFromByteArrayAsync(dataFiles, 0, dataFiles.Length);
            return RedirectToAction("ShowAllProfiles", "StorageUpload");
        }

        public async Task<IActionResult> ShowAllProfiles()
        {
            string blobstorageconnection = _configuration.GetValue<string>("blobstorage");
            string containerName = _configuration.GetValue<string>("ContainerName");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);           
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlobDirectory dirb = container.GetDirectoryReference(containerName);

            BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(string.Empty,
                true, BlobListingDetails.Metadata, 100, null, null, null);
            List<BlobData> fileList = new List<BlobData>();

            foreach (var blobItem in resultSegment.Results)
            {             
                var blob = (CloudBlob)blobItem;
                fileList.Add(new BlobData()
                {
                    FileName = blob.Name,
                    FileSize = Math.Round((blob.Properties.Length / 1024f) / 1024f, 2).ToString(),
                    ModifiedOn = DateTime.Parse(blob.Properties.LastModified.ToString()).ToLocalTime().ToString()
                });
            }
            return View(fileList);
        }

        public async Task<IActionResult> Download(string blobName)
        {
            CloudBlockBlob blockBlob;
            await using (MemoryStream memoryStream = new MemoryStream())
            {
                string blobstorageconnection = _configuration.GetValue<string>("blobstorage");
                string containerName = _configuration.GetValue<string>("ContainerName");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                await blockBlob.DownloadToStreamAsync(memoryStream);
            }

            Stream blobStream = blockBlob.OpenReadAsync().Result;
            return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        }

        public async Task<IActionResult> Delete(string blobName)
        {
            string blobstorageconnection = _configuration.GetValue<string>("blobstorage");
            string containerName = _configuration.GetValue<string>("ContainerName");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();            
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            var blob = cloudBlobContainer.GetBlobReference(blobName);
            await blob.DeleteIfExistsAsync();
            return RedirectToAction("ShowAllProfiles", "StorageUpload");
        }
    }
}
