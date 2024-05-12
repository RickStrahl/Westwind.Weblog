using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Markdig;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Westwind.AspNetCore;
using Westwind.AspNetCore.Extensions;
using Westwind.Utilities;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Models;
using ILogger = Castle.Core.Logging.ILogger;

namespace Westwind.Weblog.Views.Home
{
    public class HomeController : WeblogBaseController
    {
        public HttpContextAccessor ContextAccessor { get; }
        public ILogger<HomeController> Logger { get; }

        public HomeController(HttpContextAccessor contextAccessor, ILogger<HomeController> logger)
        {
            ContextAccessor = contextAccessor;
            Logger = logger;
        }

        public IActionResult MissingPage(string path, string url = null)
        {
            var model = new ErrorViewModel();
            model.Path = url;
            InitializeViewModel(model);

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Error(Exception ex = null)
        {
            var mainException = ex;
            var pathException = ex;

            IExceptionHandlerPathFeature exceptionHandlerPath = null;
            IExceptionHandlerFeature exceptionHandler = null;

            if (ex != null)
            {
                exceptionHandlerPath = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                exceptionHandler = HttpContext.Features.Get<IExceptionHandlerFeature>();

                mainException = exceptionHandler?.Error;
                pathException = exceptionHandlerPath?.Error;
            }

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Error = pathException,
                StatusCode = (int)HttpStatusCode.InternalServerError
            };

            var context = ContextAccessor.HttpContext;

            var header = StringUtils.GetLines(mainException.Message).FirstOrDefault();

            if (pathException is HttpRequestException httpEx)
            {
                model.StatusCode = (int)httpEx.StatusCode.Value;
                Response.StatusCode = (int)model.StatusCode;
            }
            else
            {
                if (context?.Request != null)
                {
                    model.HttpVerb = context?.Request?.Method?.ToString();

                    if (context != null &&
                        model.HttpVerb.Equals("post", StringComparison.InvariantCultureIgnoreCase) ||
                        model.HttpVerb.Equals("put", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            model.PostData = await context.Request.GetRawBodyStringAsync();
                        }
                        catch
                        {
                        }

                        if (model.PostData is { Length: > 1000 })
                        {
                            model.PostData = model.PostData.GetMaxCharacters(1000);
                        }
                    }
                }

                var msg = $@"
<style>
body {{ font-family: sans-serif }}
</style>
# {header}

**({(int)model.StatusCode}) {model.HttpVerb} {exceptionHandlerPath?.Path}**

```
{mainException.StackTrace}*
```
{model.PostData}
---
";

                var textMsg = $@"{header}
({(int)model.StatusCode}) {model.HttpVerb} {exceptionHandlerPath?.Path}

{mainException.StackTrace}

{model.PostData}
";
                
                Logger.LogCritical(pathException, textMsg);

                if (wlApp.Configuration.Email.SendAdminEmails)
                {
                    //AppWebUtils.SendEmail(wlApp.Configuration.Email.AdminSenderEmail,
                    //    $"Web Store Error: {model.StatusCode} " + exceptionHandlerPath?.Path,
                    //    Markdown.Parse(msg),
                    //     out string error, noHtml: false);
                }
            }


            InitializeViewModel(model);

            return View(model);
        }
    }
}
