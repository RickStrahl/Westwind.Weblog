using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BlazePostApi.Rss;
using Westwind.AspNetCore.Extensions;
using Westwind.AspNetCore.Markdown;
using Westwind.Utilities;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Webstore.Business.Utilities;


namespace Westwind.Weblog.Views.Home
{
    public class HomeController : WeblogBaseController
    {                
        public ILogger<HomeController> Logger { get; }        

        public HomeController( ILogger<HomeController> logger)
        {
            Logger = logger;
        }
                      

        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        [Route("/rss")]
        public async Task<ActionResult> RssFeed([FromQuery] bool force, [FromServices] PostBusiness postBusiness)
        {
            var config = postBusiness.Configuration;

            var rssFeed = new RssFeed()
            {
                Title = config.ApplicationName,
                Link = config.WeblogHomeUrl,
                Copyright = "(c) West Wind Technologies 2006-" + DateTime.Now.Year,
                Description = "Wind, waves, code and everything in between",
                Generator = "Rick Strahl's West Wind Weblog",
                PubDate = DateTime.UtcNow,
                ImageUrl = config.WeblogRssImageUrl
            };

            var posts = await postBusiness.GetLastPostsAsync(10, includeBody: true);
            var lastPost = posts.FirstOrDefault();
            if (lastPost != null)
                rssFeed.LastUpdate = lastPost.Created.ToUniversalTime();

            int count = 0;
            foreach (var post in posts)
            {
                count++;

                var rssItem = new RssItem()
                {
                    Title = post.Title,
                    CommentCount = post.CommentCount,
                    Link = post.GetPostUrl(),
                    Permalink = post.GetPostUrl(),
                    PublishDate = post.Created,
                    Guid = post.Id.ToString()
                };
                rssItem.Author.Name = post.Author ?? config.WeblogAuthor;
                rssItem.CommentsUrl = rssItem.Link + "#Comments";


                if (!string.IsNullOrEmpty(post.Categories))
                    rssItem.Categories = post.Categories
                                    .Split('.', StringSplitOptions.RemoveEmptyEntries)
                                    .ToList();


                //string body = StringUtils.ReplaceStringInstance(post.Body, "##AD##", App.SponsorSquareAd, 1, true);

                string body = post.Body;
                if (!string.IsNullOrEmpty(body))
                    body = body
                        //.Replace("##AD##", "")
                        .Replace("##PAGEBREAK##", "");

                body = postBusiness.EmbedAds(body);

                if (count > 3)
                    body = post.Abstract ?? StringUtils.TextAbstract(body, 250);

                rssItem.Body = body;
                rssItem.Abstract = post.Abstract;
                rssItem.FeaturedImage = post.FeaturedImageUrl;

                rssFeed.Items.Add(rssItem);               
            }

            return Content(rssFeed.SerializeToString(), "text/xml"); // new MediaTypeHeaderValue("text/xml"));
        }

        public IActionResult MissingPage(string path, string url = null)
        {
            var model = new ErrorViewModel();
            model.Path = url;
            InitializeViewModel(model);

            return View(model);
        }

        [Route("/home/error")]
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

            var context =this.HttpContext; // ContextAccessor.HttpContext;

            

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
                    var emailer = new Emailer();
                    emailer.SendEmail(wlApp.Configuration.Email.SenderEmail,
                        $"Web Store Error: {model.StatusCode} " + exceptionHandlerPath?.Path,
                        Markdown.Parse(msg), EmailModes.html);
                }
            }


            InitializeViewModel(model);

            return View("Error", model);
        }
    }
}
