using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Westwind.AspNetCore.Extensions;
using Westwind.AspNetCore.Utilities;
using Westwind.Utilities;
using Westwind.Utilities.Data;
using Westwind.Web;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    [Authorize(Roles = "Admin")]
    public class AdminController : WeblogBaseController
    {
        WeblogConfiguration Configuration { get; }
        public IWebHostEnvironment Host { get; }

        AdminBusiness AdminRepo { get; }
        
        
        
        public AdminController(AdminBusiness repo, 
                               WeblogConfiguration configuration,
                               IWebHostEnvironment host)
        {
            Configuration = configuration;
            Host = host;
            AdminRepo = repo;
            
        }

        [Route("/admin")]
        [Route("/admin/index")]
        [HttpGet]        
        public IActionResult Index()
        {
            var model = CreateViewModel<AdminViewModel>();
            return View(model);
        }

        [HttpGet("/admin/import")]
        public IActionResult Import()
        {
            var model = CreateViewModel<AdminViewModel>();
            model.Message = !AdminRepo.ImportOldWebLog(wlApp.Configuration.OldWeblogConnectionString) 
                    ? AdminRepo.ErrorMessage 
                    : "Import completed.";

            return View("Index",model);
        }

        [Route("admin/deleteunusedimages")]
        public IActionResult DeleteUnusedImages()
        {
            var model = CreateViewModel<AdminViewModel>();
            model.Message = "Unused Images updated.";

            var sb = AdminRepo.DeleteOldImages(Path.Combine(Host.WebRootPath, "images"));
            if (sb == null)
                model.Message = "Image deletion failed: " + AdminRepo.ErrorMessage;
            else
            {
                model.Message = $"{StringUtils.CountLines(sb.ToString())} images deleted.\r\n<pre>{sb}</pre>";
            }

            return View("Index",model);
        }

        [Route("admin/updatecommentcounts")]
        public IActionResult UpdateCommentCounts()
        {
            var model = CreateViewModel<AdminViewModel>();
            if (!AdminRepo.UpdatePostCommentCounts())
            {
                model.Message = "Comment updates failed: " + AdminRepo.ErrorMessage;
            }
            else
                model.Message = "Comment counts updated.";

            return View("Index",model);
        }

        [Route("admin/backup")]
        public IActionResult Backup()
        {
            var model = CreateViewModel<AdminViewModel>();

            string basePath = HttpContext.MapPath("~/admin/temp/");
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            var sql = new SqlDataAccess(wlApp.Configuration.ConnectionString);
            sql.Timeout = 180;

            var dirInfo = new DirectoryInfo(basePath);
            foreach (var file in dirInfo.GetFiles("weblog-*.*"))
            {
                file.Delete();
            }

            string baseFileName = "weblog-backup-" + DateTime.Now.ToString("yyyy-MM-dd");
            string backupFile = basePath + baseFileName + ".bak";
            int res = sql.ExecuteNonQuery("backup database weblog to DISK = @0", backupFile);

            if (res < 0)
            {
                ErrorDisplay.ShowError("Backup failed: " + sql.ErrorMessage);
            }
            else
            {
                string fullFile = basePath + "..\\" + baseFileName + ".zip";
                string outputFile = basePath + baseFileName + ".zip";
                System.IO.File.Delete(fullFile);
                ZipFile.CreateFromDirectory(basePath, fullFile);
                System.IO.File.Move(fullFile, outputFile);

                //return File(outputFile, "application/zip", baseFileName + ".zip");
                ErrorDisplay.MessageAsRawHtml = true;
                ErrorDisplay.ShowSuccess("Backup succeeded<hr><a href='" + WebUtils.ResolveUrl("~/admin/temp/" + baseFileName + ".zip") + "'>Download</a>");
            }
            
            return View("Index",model);
        }

        [HttpGet("/admin/ads")]
        public IActionResult Ads()
        {
            var model = CreateViewModel<AdsViewModel>();
            LoadAdsFromXml(model);
            return View(model);
        }

        [HttpPost("/admin/ads")]
        public IActionResult Ads(AdsViewModel posted)
        {
            var model = CreateViewModel<AdsViewModel>();
            model.BottomPostAd = posted.BottomPostAd;
            model.TopPostAd = posted.TopPostAd;
            model.TopPageAd = posted.TopPageAd;
            model.ContentAds = posted.ContentAds?
                .Where(a => !string.IsNullOrWhiteSpace(a)).ToList() ?? new List<string>();
            model.SponsorBanners = posted.SponsorBanners?
                .Where(b => !string.IsNullOrWhiteSpace(b)).ToList() ?? new List<string>();

            if (SaveAdsToXml(model))
            {
                AdManager.Reload();
                model.Message = "Ads saved successfully.";
            }
            else
            {
                model.IsError = true;
                model.Message = "Failed to save ads — check file permissions.";
            }

            return View(model);
        }

        private void LoadAdsFromXml(AdsViewModel model)
        {
            var path = Path.Combine(Host.WebRootPath, "admin", "adsnew.xml");
            if (!System.IO.File.Exists(path)) return;

            var root = XDocument.Load(path, LoadOptions.PreserveWhitespace).Root;
            if (root == null) return;

            model.BottomPostAd = root.Element("BottomPostAd")?.Value ?? string.Empty;
            model.TopPostAd    = root.Element("TopPostAd")?.Value    ?? string.Empty;
            model.TopPageAd    = root.Element("TopPageAd")?.Value    ?? string.Empty;

            model.SponsorBanners.AddRange(
                root.Element("SponsorBanners")?
                    .Elements("Banner").Select(e => e.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                ?? Enumerable.Empty<string>());

            model.ContentAds.AddRange(
                root.Element("ContentAds")?
                    .Elements("Ad").Select(e => e.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                ?? Enumerable.Empty<string>());
        }

        private bool SaveAdsToXml(AdsViewModel model)
        {
            try
            {
                var path = Path.Combine(Host.WebRootPath, "admin", "adsnew.xml");
                var doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("Ads",
                        new XElement("BottomPostAd", model.BottomPostAd ?? string.Empty),
                        new XElement("TopPostAd",    model.TopPostAd    ?? string.Empty),
                        new XElement("TopPageAd",    model.TopPageAd    ?? string.Empty),
                        new XElement("SponsorBanners",
                            model.SponsorBanners.Select(b => new XElement("Banner", b))),
                        new XElement("ContentAds",
                            model.ContentAds.Select(a => new XElement("Ad", a)))));
                doc.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AdminViewModel : WeblogBaseViewModel
    {
        public string Message { get; set; }
    }

    public class AdsViewModel : WeblogBaseViewModel
    {
        public string Message { get; set; }
        public bool IsError { get; set; }
        public string BottomPostAd { get; set; }
        public string TopPostAd { get; set; }
        public string TopPageAd { get; set; }
        public List<string> ContentAds { get; set; } = new();
        public List<string> SponsorBanners { get; set; } = new();
    }
}