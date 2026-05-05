using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Westwind.AspNetCore.Errors;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

using Westwind.WeblogPostService.Model;
using Westwind.WeblogServices.Client;
using Westwind.WeblogServices.Server;

namespace Westwind.AspNetCore.Controllers
{





    /// <summary>
    /// Handles upload and download of posts, media objects and categories.
    /// </summary>    
    [Route("/api/publish")]
    public class WeblogPostService : WeblogPostServiceBase
    {
        private readonly PostBusiness PostBusiness;
        UserBusiness UserBusiness { get;  }
        IWebHostEnvironment Host { get; }

        public WeblogPostService(UserBusiness userBus, 
            PostBusiness postBusiness, 
            IWebHostEnvironment host) : base(wlApp.Configuration.ConnectionString)
        {
            PostBusiness = postBusiness;
            UserBusiness = userBus;
            Host = host;            
        }

            

        [HttpPost]
        [Route("authenticate")]
        public override WeblogTokenInfo Authenticate([FromBody] AuthenticateRequest auth)
        {
            var user = UserBusiness.AuthenticateAndRetrieveUser(auth.Username, auth.Password);
            if (user == null)
                throw new ApiException("Invalid Username or Password.", 401);

            var tokenString = CreateNewToken(user.Id);
            if (string.IsNullOrEmpty(tokenString))
                throw new ApiException("Failed to create authentication token.", 401);

            return new WeblogTokenInfo { Token = tokenString };
        }

        [HttpPost]
        [Route("")]
        public override WeblogPost UploadPost([FromBody] WeblogPost post)
        {
            var postId = post.PostId;


            Post lastPost = null;
            Post newPost = null;

            bool isNewPost = false;

            if (!string.IsNullOrEmpty(postId))
            {                
                newPost = PostBusiness.Load(postId);
            }

            if (newPost == null)
            {
                newPost = PostBusiness.Create();
                lastPost = PostBusiness.LoadLastPost();
                newPost.Location = lastPost.Location;
                isNewPost = true;
            }

            newPost.Title = post.Title;
            newPost.Body = post.Body;
            newPost.Abstract = post.Abstract;
            newPost.Markdown = post.RawPostText;
            newPost.Author = post.Author;            
            newPost.Active = post.PostStatus == PostStatuses.Published;
            newPost.FeaturedImageUrl = post.ImageUrl;

            
            if (string.IsNullOrEmpty(newPost.SafeTitle))
                newPost.SafeTitle = PostBusiness.GetSafeTitle(newPost.Title);

            if (!string.IsNullOrEmpty(post.Location))
                newPost.Location = post.Location;
            else if (isNewPost && lastPost != null)  // new post
                newPost.Location = lastPost.Location;
            if (!string.IsNullOrEmpty(newPost.Author))
                newPost.Author = UserBusiness.Configuration.WeblogAuthor;
            
            newPost.Keywords = post.Keywords;
            newPost.Categories = string.Join( ',', post.Categories);
            
            if (newPost.Created.Year < 2000)
                newPost.Created = post.DateCreated;

            if (post.CustomFields.Count > 0)
            {
                // Pass mt_updateslug to force the slug to refresh itself based on the title
                // and created date.
                var kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_updateslug");
                if (kvl.Key != null)
                    newPost.SafeTitle = PostBusiness.GetSafeTitle(newPost.Title);

                // Update the created date which also changes the slug
                // Use with caution: This will change the URL
                kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_date");
                if (kvl.Key != null)
                {
                    DateTime.TryParse(kvl.Value, out DateTime dt);
                    if (dt > DateTime.MinValue)
                    {
                        newPost.Created = dt;
                        newPost.Updated = DateTime.Now;
                        newPost.SafeTitle = PostBusiness.GetSafeTitle(newPost.Title); // have to update the slug
                    }
                }
            }

            if (!PostBusiness.Save(newPost))
                throw new ApiException(PostBusiness.ErrorMessage);

            post.FromPost(newPost);

            return post;
        }

        [HttpPost]
        [Route("media")]
        public override string UploadMediaObject([FromBody] MediaObject media)
        {
            var imagePath = Url.Content("~/images/") + DateTime.Now.Year;
            var rootPath = Host.WebRootPath;

            string ImagePhysicalPath = Path.Combine(rootPath, "images",DateTime.Now.Year.ToString()) + Path.DirectorySeparatorChar;
            string ImageWebPath = Request.Scheme + "//" + Request.Host + imagePath;

            if (media.Data != null)
            {

                // we only allow images
                using (MemoryStream ms = new MemoryStream(media.Data))
                {
                    var buffer = new byte[20];
                    var size= ms.Read(buffer , 0, 20);


                    using (Bitmap bitmap = new Bitmap(ms))
                    {
                        if (bitmap == null || bitmap.Width < 1)
                            throw new ApiException("Only image uploads are allowed.",401);
                    }
                }

                ImagePhysicalPath = Path.Combine(ImagePhysicalPath, media.Name);
                string PathOnly = Path.GetDirectoryName(ImagePhysicalPath);
                if (!Directory.Exists(PathOnly))
                    Directory.CreateDirectory(PathOnly);

                // TODO: Validate Image by loading into Image Class
                System.IO.File.WriteAllBytes(ImagePhysicalPath,media.Data);
                
                // TODO: Pack down Images
                //if (Path.GetExtension(ImagePhysicalPath).ToLower() == ".png")
                //{
                //    var pngOutPath = HttpContext.Current.Server.MapPath("~/") + "tools\\pngout.exe";
                //    var p = Process.Start(pngOutPath, "\"" + ImagePhysicalPath + "\"");
                //    p.ErrorDataReceived += (sender, e) =>
                //    {
                //        LogManager.Current.LogError("pngOut failed", e.Data);
                //    };
                //}
            }

            var url = ImageWebPath + "/" + media.Name;
            url = url.Replace(" ", "%20");
            return url;            
        }

        [Route("{postId}/{blogId?}")]
        public override WeblogPost GetPost(string postId, string blogId)
        {
            if (string.IsNullOrEmpty(postId))
                throw new ApiException("Invalid PostId. Please make sure you provide an Id of an existing post.", 400);
            
            var post = PostBusiness.Load(postId);
            if (post == null)
                throw new ApiException("Post not found.", 404);

            var blogPost = new WeblogPost()
            {
                BlogId = "1", // only one blog so we hardcode this
                PostId = post.Id.ToString(),
                Abstract = post.Abstract,
                Title = post.Title,
                Body = post.Body,
                RawPostText = post.Markdown,
                RawPostType = post.BodyMode == 2 ? "markdown" : "html",
                DateCreated = post.Created,
                Location = post.Location,
                Url = PostBusiness.GetPostUrl(post),
                PostStatus = post.Active ? PostStatuses.Published : PostStatuses.Draft,
            };
            blogPost.PermaLink = blogPost.Url;

            if (!string.IsNullOrEmpty(post.Categories))
                blogPost.Categories = post.Categories.Split(',')?.ToList() ?? [];

            blogPost.Comments = PostBusiness.Context.Comments?
                .Where(c => c.PostId == postId)
                .Select(c => new Comment
                {
                    Id = c.Id.ToString(),
                    PostId = c.PostId,
                    Author = c.Author,
                    Title = c.Title,
                    Body = c.Body,
                    Created = c.Created,
                    Email = c.Email,
                    Url = c.Url,
                    BodyMode = c.BodyMode,
                    IsActive = c.IsActive
                }).ToList() ?? [];

            return blogPost;
        }

        [HttpPost]
        [Route("recent")]
        public override IList<WeblogMinimalPost> GetRecentPosts([FromBody] PostListFilter listFilter)
        {            
            if (listFilter == null)
            {
                listFilter = new PostListFilter();
            }

            var posts = PostBusiness.GetLastPosts(listFilter.NumberOfPosts, listFilter.IncludeBody);

            var postList = new List<WeblogMinimalPost>();
            foreach (var post in posts)
            {
                var weblogPost = new WeblogMinimalPost()
                {
                    PostId = post.Id,
                    Title = post.Title,
                    Abstract = post.Abstract,
                    Created = post.Created,
                    Location = post.Location,
                    Url = wlApp.Configuration.WeblogHomeUrl?.TrimEnd('/') + PostBusiness.GetPostUrl(post),
                    FeaturedImageUrl = post.FeaturedImageUrl,
                    CommentCount = post.CommentCount,
                };
                if (listFilter.IncludeBody)
                    weblogPost.Body = post.Body;
                postList.Add(weblogPost);
            }
            
            return postList;
        }

        //[AllowAnonymous]
        //[HttpGet]
        //[Route("/rss")]
        //public async Task<ActionResult> RssFeed(bool force)
        //{
        //    var config = PostBusiness.Configuration;

        //    var rssFeed = new RssFeed()
        //    {
        //        Title = config.ApplicationName,
        //        Link = config.WeblogHomeUrl,
        //        Copyright = "(c) West Wind Technologies 2006-" + DateTime.Now.Year,
        //        Description = "Wind, waves, code and everything in between",
        //         Generator = "Rick Strahl's West Wind Weblog"    ,
        //        PubDate = DateTime.UtcNow,
        //        ImageUrl = config.WeblogImageUrl                                                
        //    };
            

        //    var posts = await PostBusiness.GetLastPostsAsync(10, includeBody: true);
        //    var lastPost = posts.FirstOrDefault();
        //    if (lastPost != null)
        //        rssFeed.LastUpdate = lastPost.Created.ToUniversalTime();

        //    int count = 0;
        //    foreach (var post in posts)
        //    {
        //        count++;

        //        var rssItem = new RssItem()
        //        {
        //            Title = post.Title,
        //            CommentCount = post.CommentCount,
        //            Link = PostBusiness.GetPostUrl(post,fullyQualified: true),                    
        //            Permalink = PostBusiness.GetPostUrl(post),
        //            PublishDate = post.Created,
        //            Guid = post.Id.ToString()
        //        };
        //        rssItem.Author.Name = post.Author ?? config.WeblogAuthor;
        //        rssItem.CommentsUrl = rssItem.Link + "#Comments";
                

        //        if (!string.IsNullOrEmpty(post.Categories))                
        //            rssItem.Categories = post.Categories
        //                            .Split('.', StringSplitOptions.RemoveEmptyEntries)
        //                            .ToList();
                
                
        //        //string body = StringUtils.ReplaceStringInstance(post.Body, "##AD##", App.SponsorSquareAd, 1, true);
                
        //        string body = post.Body;
        //        if (!string.IsNullOrEmpty(body))
        //            body = body
        //                .Replace("##AD##", "")
        //                .Replace("##PAGEBREAK##", "");

        //        if (count > 3)
        //            body = post.Abstract ?? StringUtils.TextAbstract(body, 250);

        //        rssItem.Body = body;

                
        //        rssFeed.Items.Add(rssItem);

                

                
        //    }
            
        //    return Content(rssFeed.SerializeToString(), new MediaTypeHeaderValue("text/xml"));
        //}

        
    }



    
}
