using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Westwind.AspNetCore.Extensions;
using Westwind.AspNetCore.Markdown;
using Westwind.AspNetCore.Messages;
using Westwind.AspNetCore.Utilities;
using Westwind.Utilities;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;
using Westwind.Weblog.Business.Utilities;
using Westwind.Webstore.Business.Utilities;

namespace Westwind.Weblog
{
    public class PostsController : WeblogBaseController
    {
        PostBusiness Postbus { get; }

        WeblogConfiguration Config  { get; }

        IMemoryCache Cache { get; }
        
        public PostsController(PostBusiness postbus, 
                               WeblogConfiguration config,
                               IMemoryCache cache)
        {
            Postbus = postbus;
            Config = config;
            Cache = cache;            
        }

        [Route("")]
        [Route("/posts")]
        public async Task<IActionResult> Index()
        {
            var posts = await Postbus.GetLastPostsAsync(Config.HomePagePostCount);
            return View(new PostViewModel { Posts = posts, PostRepo = Postbus });
        }



        //[Route("ShowPost.aspx?id={id:int}")]
        [HttpGet]
        [Route("/posts/{id}")]
        [Route("/posts/{year:int}/{month}/{day:int}/{slug}")]
        public async Task<IActionResult> ShowPost(int year, string month, int day, string slug, object html, string id= null)
        {
            Post post;
            if (!string.IsNullOrEmpty(id))
            {
                post = await Postbus.GetPost(id);
                if (post != null)
                    return RedirectPermanent(post.GetPostUrl());
            }
            else
                post = await Postbus.GetPost(slug);

            if (post == null)
            {
                return Redirect("/");                
            }

            // Markdown
            string postHtml = post.BodyMode == 2 ? Markdown.Parse(post.Markdown) : post.Body; // html already rendered                       
            postHtml = Postbus.EmbedAds(postHtml);

            var page = Request.Query["page"].FirstOrDefault();
            int.TryParse(page, out int pageToDisplay);
            if (pageToDisplay < 1)
                pageToDisplay = 1;

            List<string> pages = new List<string>();
            if (post.Body.Contains("#PAGEBREAK"))
                pages = post.Body.Split(new[] {"#PAGEBREAK"}, StringSplitOptions.RemoveEmptyEntries).ToList();
            if(pages.Count < 1)
                pages.Add(post.Body);

            var totalPages = pages.Count;
            if (totalPages == 0)
                totalPages = 1;

            if (pageToDisplay <= totalPages)
                post.Body = pages[pageToDisplay - 1];
            else
                totalPages = 1;


            // Message from previous Post request to display - comment moderation after approval
            string commentMessage = TempData["CommentMessage"]?.ToString();
            if (!string.IsNullOrEmpty(commentMessage))
            {

                ErrorDisplay.ShowWarning(commentMessage, "Comment Moderation");
            }
            else
            {
                if (CanLogRequest())
                {
                    RequestLogger.LogRequest(post.Id, Request.Headers?.Referer, Request.GetClientIpAddress()).FireAndForget();
                }
            }

            var cats = post.Categories?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToArray() ?? [];
            var relatedPosts = Postbus.GetRelatedPosts(cats, 5, post.Id) ?? [];


            return View(new PostViewModel { PostHtml = postHtml, Post = post, PostRepo = Postbus, 
                                            RelatedPosts = relatedPosts,
                                            PageToDisplay = pageToDisplay, 
                                            TotalPages = totalPages, 
                                            ErrorDisplay = ErrorDisplay });
        }


        /// <summary>
        /// Post and Save Comment 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="slug"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/posts/{id}")]
        [Route("/posts/{year:int}/{month}/{day:int}/{slug}")]
        [Route("showpost.aspx")]
        public async Task<IActionResult> ShowPostPost([FromForm] PostViewModel model, [FromRoute] int year, [FromRoute] string month, [FromRoute] int day, [FromRoute] string slug, [FromRoute] string id = null)
        {
            Post post;
            if (!string.IsNullOrEmpty(id))
                post = await Postbus.GetPost(id);
            else
                post = await Postbus.GetPost(slug);


            var page = Request.Query["page"].FirstOrDefault();
            int.TryParse(page, out int pageToDisplay);
            if (pageToDisplay < 1)
                pageToDisplay = 1;

            List<string> pages = new List<string>();
            if (post.Body.Contains("#PAGEBREAK"))
                pages = post.Body.Split(new[] { "#PAGEBREAK" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (pages.Count < 1)
                pages.Add(post.Body);

            var totalPages = pages.Count;
            if (totalPages == 0)
                totalPages = 1;

            if (pageToDisplay <= totalPages)
                post.Body = pages[pageToDisplay - 1];
            else
                totalPages = 1;

            // Markdown
            string postHtml = post.BodyMode == 2 ? Markdown.Parse(post.Markdown) : post.Body; // html already rendered                       
            postHtml = Postbus.EmbedAds(postHtml);

            model.Post = post;
            model.PostRepo = Postbus;

            var newModel = new PostViewModel { PostHtml = postHtml, Post = post, ActiveComment = model.ActiveComment, PostRepo = Postbus, PageToDisplay = pageToDisplay, TotalPages = totalPages };
            InitializeViewModel(newModel);

            
            
            var actionResult = await HandleComment(newModel, post);
            if (actionResult != null)
                return actionResult;

            return View("ShowPost", newModel);
                                    
        }

        public async Task<IActionResult> HandleComment(PostViewModel newModel, Post post)
        {
            var comment = newModel.ActiveComment;
         

            comment.IsCommentDialogVisible = true;
            comment.Post = post;
            comment.CommentErrorMessage = HttpContext.Items["CommentMessage"]?.ToString();



            // posting back
            if (string.IsNullOrEmpty(comment.CommentAuthor))
                comment.CommentAuthor = Request.Cookies["CommentAuthor"];
            if (string.IsNullOrEmpty(comment.CommentEmail))
                comment.CommentEmail = Request.Cookies["CommentEmail"];
            if (string.IsNullOrEmpty(comment.CommentWebSite))
                comment.CommentWebSite = Request.Cookies["CommentWebSite"];

            if (!string.IsNullOrEmpty(comment.CommentText))
            {

                var dataComment = Postbus.Create<Comment>();
                dataComment.Body = comment.CommentText;
                dataComment.Title = "re: " + comment.Post.Title;
                dataComment.Author = comment.CommentAuthor;
                dataComment.Email = comment.CommentEmail;
                dataComment.Url = comment.CommentWebSite;

                // TODO: Make this False and manually require enabling
                dataComment.IsActive = false;

                post.Comments.Add(dataComment);

                var hasError = false;
                if (!Postbus.ValidateComment(dataComment))
                {
                    hasError = true;
                    ErrorDisplay.ShowError(Postbus.ValidationErrors.ToHtml(), "Please fix the following:");
                }

                if (!string.IsNullOrEmpty(wlApp.Configuration.CommentAutoApproveNamePart) &&
                   (comment.CommentEmail?.Contains(wlApp.Configuration.CommentAutoApproveNamePart) ?? false))
                {
                    dataComment.IsActive = true;  // Auto approve
                }

                if (!hasError && await Postbus.SaveAsync(post))
                {
                    ModelState.Clear();
                    HttpContext.Items["CommentMessage"] = "Comment has been saved, but comment moderation is enabled, so it won't display until approved. Please check back later.";


                    var options = new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7), Domain = Request.Host.Host, HttpOnly = true, Secure = true, Path = "/" };
                    Response.Cookies.Append("CommentAuthor", comment.CommentAuthor ?? string.Empty, options);
                    Response.Cookies.Append("CommentEmail", comment.CommentEmail ?? string.Empty, options);
                    Response.Cookies.Append("CommentWebSite", comment.CommentWebSite ?? string.Empty, options);

                    if (wlApp.Configuration.Email.SendEmails)
                    {
                        var siteUrl = wlApp.Configuration.ApplicationBasePath;


                        string CommentBody = "<div style='font: normal normal 10pt Verdana'>" +
                                             "Title: re: " + post.Title + "<br />" +
                                             "From: " + comment.CommentAuthor + "<br />" +
                                             "Url: " + HtmlUtils.Href(comment.CommentWebSite) + "<br />" +
                                             "Email: " + comment.CommentEmail + "<br />" +
                                             "IP: " + Request.GetClientIpAddress() + "<br /><hr />" +
                                             WebUtility.HtmlEncode(comment.CommentText);
                        CommentBody += "<br /><br /><small>" +
                                       HtmlUtils.Href("Show Comment",
                                           post.GetPostUrl() + "#" + dataComment.Id) + " | " +
                                       HtmlUtils.Href("Remove Comment", siteUrl.TrimEnd('/') + "/comments/" + dataComment.Id + "/remove") + " | " +
                                   HtmlUtils.Href("Approve Comment", siteUrl.TrimEnd('/') + "/comments/" + dataComment.Id + "/approve") +
                                   "</small></div>";

                        

                        Task.Run(() =>
                        {
                            // Send admin a notification
                            var emailer = new Emailer();                           
                            bool result = emailer.SendEmail(wlApp.Configuration.Email.SenderEmail,
                                                            "Weblog Comment: " + post.Title,
                                                            CommentBody, EmailModes.html);
                        }).FireAndForget();
                    }

                    comment.CommentText = null;
                    var message = "Your comment has been saved, but comment moderation is enabled which may cause a delay until your comment is displayed.";
                    TempData["CommentMessage"] = message;

                    return Redirect(post.GetPostUrl() + "#Comments");
                }

                ErrorDisplay.MessageAsRawHtml = true;
                ErrorDisplay.ShowError($"{Postbus.ErrorMessage}", "Couldn't save comment");
            }

            return null;
        }

        [Route("/comments")]
        public async Task<IActionResult> RecentComments()
        {
            var comments = await Postbus.GetRecentCommentsAsync(Config.HomePagePostCount);
            var model = new PostViewModel { Comments = comments, PostRepo = Postbus };
            InitializeViewModel(model);
            return View(model);
        }

        [Authorize]
        [Route("/comments/{commentId}/approve")]    
        public IActionResult ApproveComment(string commentId)
        {

            var comment = Postbus.Context.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null) 
                return Json( new ApiResponse<bool> { IsError = true, Message = "Comment not found", Data = false });

            var result = new ApiResponse<bool>();
            comment.IsActive = true;
            result.Data = Postbus.Save(); // Context.SaveChanges() == 1;


            if (!Request.Headers.Accept.Any(h => h.Contains("application/json")))
            {
                var post = Postbus.Load(comment.PostId);
                var url = post != null ? post.GetPostUrl() + "#Comments": "#Comments";
                return Redirect(url);
            }

            return Json(result);
        }


        /// <summary>
        /// Returns ApiResponse bool, but redirects if accessed
        /// without an accept header.
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        [Authorize]
        [Route("/comments/{commentId}/remove")]
        public IActionResult RemoveComment(string commentId)
        {
            var comment = Postbus.Context.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
                return Json(new ApiResponse<bool> { IsError = true, Message = "Comment not found", Data = false });

            Postbus.Context.Remove<Comment>(comment);

            var res = Postbus.Context.SaveChanges();

            if (!Request.Headers.Accept.Any(h => h.Contains("application/json")))
            {
                var post = Postbus.Load(comment.PostId);
                var url = post != null ? post.GetPostUrl() + "#Comments" : "#Comments";
                return Redirect(url);
            }

            return Json(new ApiResponse<bool> { Data = true });
        }

        [Route("/comments/format")]
        public string FormatComment(string commentText)
        {
            return Markdown.Parse(commentText);            
        }

        /// <summary>
        /// Returns ApiResponse bool, but redirects if accessed
        /// without an accept header.
        /// </summary>
        /// <param name="commentId"></param>
        /// <returns></returns>
        [Authorize]
        [Route("/posts/{postId}/delete")]
        public IActionResult DeletePost(string postId)
        {
            var post = Postbus.Context.Posts.FirstOrDefault(c => c.Id == postId);
            if (post == null)
                return Json(new ApiResponse<bool> { IsError = true, Message = "Post not found", Data = false });

            Postbus.Context.Remove<Post>(post);
            var res = Postbus.Context.SaveChanges();          

            if (!Request.Headers.Accept.Any(h => h.Contains("application/json")))
            {
                var url = "/";
                return Redirect(url);
            }

            return Json(new ApiResponse<bool> { Data = true });
        }


        /// <summary>
        /// API endpoint for post search autocomplete
        /// </summary>
        [HttpGet]
        [Route("api/posts/search")]
        public async Task<IActionResult> PostSearch(string search, int count = 15)
        {
            if (string.IsNullOrWhiteSpace(search))
                return Json(new List<object>());

            var results = await Postbus.PostSearchAsync(search, count);
            return Json(results);
        }

        private bool CanLogRequest()
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            if (string.IsNullOrEmpty(userAgent) || !userAgent.Contains("Mozilla/"))
                return false;

            foreach (var bot in KnownBotUserAgents)
            {
                if (userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            string referer = Request.Headers.Referer.FirstOrDefault();
            if (string.IsNullOrEmpty(referer))
                return false;

            string accept = Request.Headers.Accept.FirstOrDefault();
            if (!accept?.Contains("text/html") ?? true)
                return false;

            return true;
        }

        private static readonly string[] KnownBotUserAgents =
        {
            "bot",
            "crawler",
            "spider",
            "slurp",
            "bingpreview",
            "facebookexternalhit",
            "facebot",
            "ia_archiver",
            "duckduckbot",
            "baiduspider",
            "yandex",
            "sogou",
            "exabot",

            // SEO / scraping bots
            "ahrefs",
            "semrush",
            "mj12bot",
            "dotbot",
            "petalbot",
            "bytespider",
            "amazonbot",
            "ccbot",

            // tools / libraries
            "curl",
            "wget",
            "python-requests",
            "httpclient",
            "go-http-client",
            "java/",
            "okhttp",
            "scrapy",
            "headless",
            "phantomjs",
            "selenium"
        };
    }
}