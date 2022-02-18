using System.Linq;
using System.Threading.Tasks;
using ProfileManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ProfileManager.Controllers
{
    public class CandidateController : Controller
    {
        private readonly CandidateDatabaseContext _context;
        private readonly IConfiguration _configuration;

        public CandidateController(CandidateDatabaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
                
        public async Task<IActionResult> Index()
        {
            return View(await _context.Candidate.ToListAsync());                
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidate
                .FirstOrDefaultAsync(m => m.ID == id);
            if (candidate == null)
            {
                return NotFound();
            }

            return View(candidate);
        }

        // GET: Todos/Create
        public IActionResult Create()
        {            
            return View();
        }
  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FirstName,LastName,Email,DoB,PlaceofBirth,ResumeID,JobDetails,CreatedDate")] Candidate candidate, IFormFile files)
        {
            string blobstorageconnection = _configuration.GetValue<string>("blobstorage");
            string containerName = _configuration.GetValue<string>("ContainerName");
            byte[] dataFiles;
            string systemFileName;         

            if (ModelState.IsValid)
            {
                systemFileName = Guid.NewGuid().ToString() + files.FileName;
                candidate.ResumeID = systemFileName;
                candidate.CreatedDate = DateTime.Now;                
                _context.Add(candidate);
                await _context.SaveChangesAsync();
                                             
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);                
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();                
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                
                await cloudBlobContainer.SetPermissionsAsync(permissions);
                await using (var target = new MemoryStream())
                {
                    files.CopyTo(target);
                    dataFiles = target.ToArray();
                }
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(systemFileName);
                await cloudBlockBlob.UploadFromByteArrayAsync(dataFiles, 0, dataFiles.Length);

                return RedirectToAction(nameof(Index));
            }
            return View(candidate);
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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidate.FindAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }
            return View(candidate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,FirstName,LastName,Email,DoB,PlaceofBirth,ResumeID,JobDetails,CreatedDate")] Candidate candidate)
        {
            if (id != candidate.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(candidate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidateExists(candidate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(candidate);
        }
               
       public async Task<IActionResult> Delete(int? id)
        {

            string blobstorageconnection = _configuration.GetValue<string>("blobstorage");
            string containerName = _configuration.GetValue<string>("ContainerName");

            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidate.FirstOrDefaultAsync(m => m.ID == id);
            if (candidate == null)
            {
                return NotFound();
            }

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            var blob = cloudBlobContainer.GetBlobReference(candidate.ResumeID);
            await blob.DeleteIfExistsAsync();
         
            return View(candidate);
            }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidate = await _context.Candidate.FindAsync(id);
            _context.Candidate.Remove(candidate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CandidateExists(int id)
        {
            return _context.Candidate.Any(e => e.ID == id);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
