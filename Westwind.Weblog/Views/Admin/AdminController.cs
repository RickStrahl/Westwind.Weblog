using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Westwind.Utilities;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    [Authorize]
    public class AdminController : Controller
    {
        WeblogConfiguration Configuration { get; }
        public IHostingEnvironment Host { get; }

        AdminBusiness AdminRepo { get; }

        AdminViewModel AdminViewModel { get;  }
        
        
        public AdminController(AdminBusiness repo, 
                               WeblogConfiguration configuration,
                               IHostingEnvironment Host)
        {
            Configuration = configuration;
            this.Host = Host;
            AdminRepo = repo;
            AdminViewModel = new AdminViewModel();
        }

        [HttpGet("Admin/Index")]        
        public IActionResult Index()
        {
            return View(AdminViewModel);
        }

        [HttpGet("admin/import")]
        public IActionResult Import()
        {
            AdminViewModel.Message = !AdminRepo.ImportOldWebLog("server=.;database=Weblog;integrated security=true;") 
                    ? AdminRepo.ErrorMessage 
                    : "Import completed.";

            return View("Index",AdminViewModel);
        }

        [Route("admin/deleteunusedimages")]
        public IActionResult DeleteUnusedImages()
        {
            AdminViewModel.Message = "Unused Images updated.";

            var sb = AdminRepo.DeleteOldImages(Path.Combine(Host.WebRootPath, "images"));
            if (sb == null)
                AdminViewModel.Message = "Image deletion failed: " + AdminRepo.ErrorMessage;
            else
            {
                AdminViewModel.Message = $"{StringUtils.CountLines(sb.ToString())} images deleted.\r\n<pre>{sb}</pre>";
            }

            return View("Index",AdminViewModel);
        }

        [Route("admin/updatecommentcounts")]
        public IActionResult UpdateCommentCounts()
        {
            if (!AdminRepo.UpdatePostCommentCounts())
            {
                AdminViewModel.Message = "Comment updates failed: " + AdminRepo.ErrorMessage;
            }
            else
                AdminViewModel.Message = "Comment counts updated.";

            return View("Index",AdminViewModel);
        }
    }

    public class AdminViewModel
    {
        public string Message { get; set; }

    }
}