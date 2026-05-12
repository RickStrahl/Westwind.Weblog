using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
        private IWebHostEnvironment Host { get; }

        private IHostApplicationLifetime AppLifeTime { get; }

        AdminBusiness AdminRepo { get; }



        public AdminController(AdminBusiness repo,
                               WeblogConfiguration configuration,
                               IWebHostEnvironment host,
                               IHostApplicationLifetime appLifeTime)
        {
            Configuration = configuration;
            Host = host;
            AdminRepo = repo;
            AppLifeTime = appLifeTime;
        }

        [Route("/admin")]
        [Route("/admin/index")]
        [HttpGet]
        public IActionResult Index()
        {
            var model = CreateViewModel<AdminViewModel>();
            return View("index", model);
        }


        [AllowAnonymous]
        [HttpGet("/admin/import")]
        public IActionResult Import()
        {
            var model = CreateViewModel<AdminViewModel>();
            model.Message = !AdminRepo.ImportOldWebLog(wlApp.Configuration.OldWeblogConnectionString)
                    ? AdminRepo.ErrorMessage
                    : "Import completed.";

            return View("Index", model);
        }


        [Route("admin/deleteunusedimages")]
        public IActionResult DeleteUnusedImages()
        {
            var model = CreateViewModel<AdminViewModel>();
            model.Message = "Unused Images updated.";

            var sb = AdminRepo.DeleteOldImages(Path.Combine(Host.WebRootPath, "images"));
            if (sb == null)
                model.ErrorDisplay.ShowError(AdminRepo.ErrorMessage, "Image deletion failed");
            else
            {
                model.ErrorDisplay.MessageAsRawHtml = true;
                model.ErrorDisplay.ShowSuccess($"{StringUtils.CountLines(sb.ToString())} images deleted.\n" + (sb.Length > 0 ? "<pre>{sb}</pre>" : null), "Image Deletion Completed");
            }

            return View("Index", model);
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

            return View("Index", model);
        }

        [Route("admin/cleanupdatabase")]
        public IActionResult CleanupDatabase()
        {
            var model = CreateViewModel<AdminViewModel>();
            if (!AdminRepo.ShrinkDatabase())
            {
                ErrorDisplay.ShowError(AdminRepo.ErrorMessage, "Database Cleanup failed");
            }
            else
            {
                ErrorDisplay.ShowSuccess("Database Cleanup Completed");
            }

            return View("Index", model);
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

            return View("Index", model);
        }


        [Route("/admin/reloadapp")]
        public IActionResult ReloadApp()
        {
            var model = CreateViewModel<AdminViewModel>();

            // touch web.config - pending permissions
            var webconfig = Path.Combine(wlApp.StartupFolder, "web.config");

            try
            {
                AppLifeTime.StopApplication();
                // var fi = new FileInfo(webconfig);
                // fi.LastWriteTime = DateTime.Now;
                ErrorDisplay.ShowSuccess("IIS Application Pool has been reloaded.");
                Response.AddMetaRefreshTagHeader("/admin", 2);
            }
            catch (Exception ex)
            {
                ErrorDisplay.ShowError(ex.Message, "IIS App reloading failed");
            }

            return View("Index", model);
        }

        [HttpGet]
        [Route("/admin/configuration")]
        public ActionResult ShowConfiguration()
        {
            var model = CreateViewModel<AdminViewModel>();
            model.ConfigurationJson = JsonSerializationUtils.Serialize(model.Configuration, false, true, false);

            return View("Configuration", model);
        }


        [HttpPost]
        [Route("/admin/configuration")]
        public ActionResult UpdateConfiguration(AdminViewModel model)
        {
            InitializeViewModel(model);

            if (Request.IsFormVar("btnUpdateConfiguration"))
            {
                var config =
                    JsonSerializationUtils.Deserialize(model.ConfigurationJson, typeof(WeblogConfiguration)) as
                        WeblogConfiguration;

                if (config != null)
                {
                    wlApp.Configuration = config;
                    model.ErrorDisplay.ShowInfo("Running configuration has been updated.");
                }
                else
                {
                    model.ErrorDisplay.ShowError("Configuration could not be updated - invalid JSON.");
                }

                // see actual current values
                ModelState.Clear();
                model.ConfigurationJson = JsonSerializationUtils.Serialize(wlApp.Configuration, false, true, false);
            }
            else if (Request.IsFormVar("btnWriteConfiguration"))
            {
                if (wlApp.Configuration.Write())
                    model.ErrorDisplay.ShowInfo($"Configuration has been written out.");
                else
                    model.ErrorDisplay.ShowError($"Configuration could not be written.");
            }

            return View("Configuration", model);
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

        [HttpGet("/admin/referrers")]
        public IActionResult Referrers()
        {
            var model = CreateViewModel<AdminViewModel>();

            var refs = AdminRepo.Referrers();
            if (refs == null)
            {
                ErrorDisplay.ShowError(AdminRepo.ErrorMessage, "Referrer Query failed");
                return View("Index",model);
            }


            return Json(refs);
        }

        [HttpGet("/admin/posthits")]
        public IActionResult PostHits()
        {
            var model = CreateViewModel<PostHitsViewModel>();
            LoadPostHitsMessage(model);
            var today = DateTime.Now.Date;

            model.SummaryRows =
            [
                CreatePostHitsSummaryRow("Last 7 days", today.AddDays(-7), today.AddDays(1), 50),
                CreatePostHitsSummaryRow("Today", today, today.AddDays(1), 50),
                CreatePostHitsSummaryRow("Yesterday", today.AddDays(-1), today, 50),
                CreatePostHitsSummaryRow("Two days ago", today.AddDays(-2), today.AddDays(-1), 50)
            ];

            if (model.SummaryRows.Any(row => row == null))
            {
                ErrorDisplay.ShowError(AdminRepo.ErrorMessage, "Post Hits Query failed");
                return View("Index", CreateViewModel<AdminViewModel>());
            }

            model.DailySections =
            [
                CreatePostHitsSection("Today", today, today.AddDays(1)),
                CreatePostHitsSection("Yesterday", today.AddDays(-1), today),
                CreatePostHitsSection("Two days ago", today.AddDays(-2), today.AddDays(-1))
            ];

            if (model.DailySections.Any(section => section == null))
            {
                ErrorDisplay.ShowError(AdminRepo.ErrorMessage, "Post Hits Query failed");
                return View("Index", CreateViewModel<AdminViewModel>());
            }

            return View(model);
        }

        [HttpGet("/admin/posthits/deleteold")]
        public IActionResult DeleteOldPostHits()
        {
            var deleted = AdminRepo.DeletePostHitsOlderThan(7);
            if (deleted < 0)
            {
                TempData["PostHitsMessage"] = null;
                TempData["PostHitsIsError"] = true;
                TempData["PostHitsError"] = AdminRepo.ErrorMessage;
            }
            else
            {
                TempData["PostHitsError"] = null;
                TempData["PostHitsIsError"] = false;
                TempData["PostHitsMessage"] = $"Deleted {deleted} hit entr{(deleted == 1 ? "y" : "ies")} older than 7 days.";
            }

            return RedirectToAction(nameof(PostHits));
        }


        private PostHitsSummaryRow CreatePostHitsSummaryRow(string label, DateTime start, DateTime end, int maxRows = 25)
        {
            var hits = AdminRepo.PostHits(start, end, maxRows);
            if (hits == null)
                return null;

            return new PostHitsSummaryRow
            {
                Label = label,
                TotalHits = hits.Sum(hit => hit.Hits),
                UrlCount = hits.Count,
                TopPostTitle = hits.FirstOrDefault()?.Title,
                TopPostUrl = hits.FirstOrDefault()?.Url,
                TopPostHits = hits.FirstOrDefault()?.Hits ?? 0
            };
        }

        private PostHitsSection CreatePostHitsSection(string label, DateTime start, DateTime end, int maxRows = 25)
        {
            var hits = AdminRepo.PostHits(start, end, maxRows);
            if (hits == null)
                return null;

            return new PostHitsSection
            {
                Label = label,
                Hits = hits
            };
        }

        private void LoadPostHitsMessage(PostHitsViewModel model)
        {
            var isError = TempData["PostHitsIsError"] as bool?;
            var message = TempData["PostHitsMessage"]?.ToString();
            var error = TempData["PostHitsError"]?.ToString();

            if (isError == true && !string.IsNullOrWhiteSpace(error))
                model.ErrorDisplay.ShowError(error, "Post hit cleanup failed");
            else if (!string.IsNullOrWhiteSpace(message))
                model.ErrorDisplay.ShowSuccess(message, "Post hits updated");
        }

        private void LoadAdsFromXml(AdsViewModel model)
        {
            var path = Path.Combine(Host.WebRootPath, "admin", "ads.xml");
            if (!System.IO.File.Exists(path)) return;

            var root = XDocument.Load(path, LoadOptions.PreserveWhitespace).Root;
            if (root == null) return;

            model.BottomPostAd = root.Element("BottomPostAd")?.Value ?? string.Empty;
            model.TopPostAd = root.Element("TopPostAd")?.Value ?? string.Empty;
            model.TopPageAd = root.Element("TopPageAd")?.Value ?? string.Empty;

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
            var ads = new AdManager
            {
                BottomPostAd = model.BottomPostAd,
                TopPostAd = model.TopPostAd,
                TopPageAd = model.TopPageAd
            };
            ads.SponsorBanners.AddRange(model.SponsorBanners);
            ads.ContentAds.AddRange(model.ContentAds);

            var path = Path.Combine(Host.WebRootPath, "admin", "ads.xml");
            return ads.SaveToXml(path);
        }
    }

    public class AdminViewModel : WeblogBaseViewModel
    {
        public string Message { get; set; }

        public string ApplicationVersion { get; } = typeof(AdminController).Assembly.GetName().Version.ToString();
        public string ApplicationDate { get; } =
            TimeUtils.FriendlyDateString(new FileInfo(typeof(wlApp).Assembly.Location).LastWriteTime);

        public string ConfigurationJson { get; set; }
    }


    public class PostHitsViewModel : WeblogBaseViewModel
    {
        public List<PostHitsSummaryRow> SummaryRows { get; set; } = new();
        public List<PostHitsSection> DailySections { get; set; } = new();
    }

    public class PostHitsSummaryRow
    {
        public string Label { get; set; }
        public int TotalHits { get; set; }
        public int UrlCount { get; set; }
        public string TopPostTitle { get; set; }
        public string TopPostUrl { get; set; }
        public int TopPostHits { get; set; }
    }

    public class PostHitsSection
    {
        public string Label { get; set; }
        public List<AdminBusiness.PostHitResult> Hits { get; set; } = new();
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
