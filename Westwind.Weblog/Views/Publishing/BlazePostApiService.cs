using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlazePostApi.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Westwind.AspNetCore.Errors;
using Westwind.Utilities;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;
using BlazePostApi;

namespace Westwind.AspNetCore.Controllers
{

    /// <summary>
    /// Handles upload and download of posts, media objects and categories.
    /// </summary>    
    [Route("/blazepostapi/")]
    public class BlazePostApiService : BlazePostApiBase
    {
        private readonly PostBusiness PostBusiness;
        UserBusiness UserBusiness { get; }
        IWebHostEnvironment Host { get; }

        public BlazePostApiService(UserBusiness userBus,
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
        [Route("post")]
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
            newPost.FeaturedImageUrl = post.FeaturedImageUrl;

            if (string.IsNullOrEmpty(newPost.SafeTitle))
                newPost.SafeTitle = PostBusiness.GetSafeTitle(newPost.Title);

            if (!string.IsNullOrEmpty(post.Location))
                newPost.Location = post.Location;
            else if (isNewPost && lastPost != null)  // new post
                newPost.Location = lastPost.Location;
            if (!string.IsNullOrEmpty(newPost.Author))
                newPost.Author = UserBusiness.Configuration.WeblogAuthor;

            newPost.Keywords = string.Join(',', post.Keywords);
            newPost.Categories = string.Join(',', post.Categories);            

            if (newPost.Created.Year < 2000)
                newPost.Created = post.DateCreated;

            if (post.CustomFields.Count > 0)
            {
                // Pass mt_updateslug to force the slug to refresh itself based on the title
                // and created date.
                var kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_updateslug");
                if (kvl.Key != null)
                {
                    newPost.SafeTitle = PostBusiness.GetSafeTitle(newPost.Title);
                    post.SafeTitle = newPost.SafeTitle;
                    newPost.PermanentUrl = null;    // force to recalculate 
                }

                kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_featuredimage");
                if (kvl.Key != null)
                {
                    newPost.FeaturedImageUrl = kvl.Value;
                    post.FeaturedImageUrl = kvl.Value;
                }

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

                kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_markdown");
                if (kvl.Key != null) 
                     newPost.Markdown = kvl.Value;
                
                kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_location");
                if (kvl.Key != null)
                    newPost.Location = kvl.Value;

                kvl = post.CustomFields.FirstOrDefault(cf => cf.Key == "mt_githuburl");
                if (kvl.Key != null)
                    newPost.GithubUrl = kvl.Value;
            }

            if (string.IsNullOrEmpty(newPost.PermanentUrl))
                newPost.PermanentUrl = newPost.GetPostUrl();
            post.Url = newPost.PermanentUrl;

            if (!PostBusiness.Save(newPost))
                throw new ApiException(PostBusiness.ErrorMessage);
            
            post.FromPost(newPost);

            return post;
        }

        [HttpPost]
        [Route("media")]
        public override string UploadMediaObject([FromBody] WeblogMediaObject weblogMedia)
        {
            var imagePath = Url.Content("~/images/") + DateTime.Now.Year;
            var rootPath = Host.WebRootPath;

            string ImagePhysicalPath = Path.Combine(rootPath, "images", DateTime.Now.Year.ToString()) + Path.DirectorySeparatorChar;
            string ImageWebPath = Request.Scheme + "://" + Request.Host + imagePath;

            if (weblogMedia.Data != null)
            {
                if (!ImageUtils.IsImage(weblogMedia.Data))
                    throw new ApiException("Only image uploads are allowed.", 401);

                ImagePhysicalPath = Path.Combine(ImagePhysicalPath, weblogMedia.Name);
                string PathOnly = Path.GetDirectoryName(ImagePhysicalPath);
                if (!Directory.Exists(PathOnly))
                    Directory.CreateDirectory(PathOnly);

                // TODO: Validate Image by loading into Image Class
                System.IO.File.WriteAllBytes(ImagePhysicalPath, weblogMedia.Data);
            }

            var url = ImageWebPath + "/" + weblogMedia.Name;
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
                FeaturedImageUrl = post.FeaturedImageUrl,
                Url = post.GetPostUrl(),
                PostStatus = post.Active ? PostStatuses.Published : PostStatuses.Draft,
            };

            if (!string.IsNullOrEmpty(post.Categories))
                blogPost.Categories = post.Categories.Split(',')?.ToList() ?? [];

            blogPost.Comments = PostBusiness.Context.Comments?
                .Where(c => c.PostId == postId)
                .Select(c => new WeblogComment
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

        [Route("last")]
        public override WeblogPost GetLastPost(string blogId)
        {

            var postId = PostBusiness.Context.Posts.OrderByDescending(p => p.Created).Select(p => p.Id).FirstOrDefault();
            if (string.IsNullOrEmpty(postId))
                throw new ApiException("No posts found.", 404);
            return GetPost(postId, blogId);
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


    }

    public static class BlazeApiExtensions
    {
        public static void FromPost(this WeblogPost weblogPost, Post post)
        {
            weblogPost.PostId = post.Id.ToString();
            weblogPost.BlogId = post.Id.ToString();
            weblogPost.PostType = "blog";
            weblogPost.Abstract = post.Abstract;
            weblogPost.Author = post.Author;
            weblogPost.Body = post.Body;
            weblogPost.Title = post.Title;
            weblogPost.DateCreated = post.Created;
            weblogPost.FeaturedImageUrl = post.FeaturedImageUrl;
            weblogPost.Location = post.Location;
            weblogPost.Categories = post.Categories.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?.ToList();
            weblogPost.Keywords = post.Keywords.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?.ToList();
            weblogPost.PermaLink = post.PermanentUrl;
            weblogPost.PostStatus = post.Active ? PostStatuses.Published : PostStatuses.Draft;
            weblogPost.SourceEditUrl = post.GithubUrl;
            weblogPost.SafeTitle = post.SafeTitle;

            weblogPost.RawPostText = post.Markdown;
            weblogPost.RawPostType = "markdown";
            weblogPost.SafeTitle = post.SafeTitle;
            

            weblogPost.Comments = post.Comments.Select(c => new WeblogComment
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
            }).ToList();
        }
    }
}

