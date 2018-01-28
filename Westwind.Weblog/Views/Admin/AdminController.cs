using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Westwind.Utilities;
using Westwind.Web;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    [Authorize]
    public class AdminController : AppBaseController
    {
        WeblogConfiguration Configuration { get; }
        public IHostingEnvironment Host { get; }

        AdminBusiness AdminRepo { get; }
        
        
        
        public AdminController(AdminBusiness repo, 
                               WeblogConfiguration configuration,
                               IHostingEnvironment Host)
        {
            Configuration = configuration;
            this.Host = Host;
            AdminRepo = repo;
            
        }

        [HttpGet("Admin/Index")]        
        public IActionResult Index()
        {
            var model = CreateViewModel<AdminViewModel>();
            return View(model);
        }

        [HttpGet("admin/import")]
        public IActionResult Import()
        {
            var model = CreateViewModel<AdminViewModel>();
            model.Message = !AdminRepo.ImportOldWebLog("server=.;database=Weblog;integrated security=true;") 
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
    }

    public class AdminViewModel : AppBaseViewModel
    {
        public string Message { get; set; }

    }
}