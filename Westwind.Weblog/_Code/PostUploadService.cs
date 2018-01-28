using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Westwind.Utilities;
using Westwind.Utilities.Logging;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Models;
using Westwind.Weblog.PostService;
using Westwind.Weblog.PostService.Model;
using WeblogPost = Westwind.Weblog.PostService.Model.WeblogPost;

namespace Westwind.AspNetCore.Controllers
{
    /// <summary>
    /// 
    /// </summary>    
    [Route("/api/posts")]
    public class PostUploadService : WeblogPostServiceBase
    {
        private readonly PostBusiness PostBusiness;
        UserBusiness UserBusiness { get;  }
        IHostingEnvironment Host { get; }

        public PostUploadService(UserBusiness userBus, 
            PostBusiness postBusiness, 
            IHostingEnvironment host)
        {
            PostBusiness = postBusiness;
            UserBusiness = userBus;
            Host = host;
        }

        public static ConcurrentDictionary<string, string> UserTokens = new ConcurrentDictionary<string, string>();

        [HttpPost]
        [Route("authenticate")]
        public override string Authenticate([FromBody] AuthenticateRequest auth)
        {
            var user = UserBusiness.AuthenticateAndRetrieveUser(auth.Username, auth.Password);
            if (user == null)
            {
                if (!string.IsNullOrEmpty(user.Username))
                {
                    var tok = UserTokens.FirstOrDefault(kv => kv.Value == user.Username);
                    UserTokens.TryRemove(tok.Key, out string t);                    
                }
                throw new UnauthorizedAccessException("Invalid Username or Password.");
            }

            var token = DataUtils.GenerateUniqueId(16);

            UserTokens[token] = user.Username;

            return token;
        }

        [HttpPost]
        [Route("")]
        public override string UploadPost([FromBody] WeblogPost post)
        {
            int.TryParse(post.PostId, out int postId);
            if (postId < 1)
                postId = 0;

            Post lastPost = null;
            if (postId == 0)
                lastPost = PostBusiness.LoadLastPost();

            var newPost = PostBusiness.Create();
            newPost.Id = postId;
            newPost.Title = post.Title;
            newPost.Body = post.Body;
            newPost.Abstract = post.Abstract;
            newPost.Markdown = post.RawPostText;
            newPost.Author = post.Author;            
            newPost.Active = post.PostStatus == PostStatuses.Published;
            newPost.ImageUrl = post.PostImageUrl;

            
            if (!string.IsNullOrEmpty(newPost.SafeTitle))
                newPost.SafeTitle = PostBusiness.GetSafeTitle(newPost.Title);

            if (string.IsNullOrEmpty(post.Location) && lastPost != null)
                newPost.Location = lastPost.Location;

            if (string.IsNullOrEmpty(newPost.Author))
                newPost.Author = UserBusiness.Configuration.WeblogAuthor;



                newPost.Keywords = post.Keywords;
            if (post.Categories.Count > 0)
                newPost.Categories = string.Join(',', post.Categories);

            if (post.DateCreated.Year > 2000)
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
                throw new InvalidOperationException(PostBusiness.ErrorMessage);

            return newPost.Id.ToString();
        }

        [HttpPost]
        [Route("image")]
        public override string UploadMediaObject([FromBody] MediaObject media)
        {
            var imagePath = Url.Content("~/images/") + DateTime.Now.Year;
            var rootPath = Host.WebRootPath;

            string ImagePhysicalPath = Path.Combine(rootPath, "images",DateTime.Now.Year.ToString()) + Path.DirectorySeparatorChar;
            string ImageWebPath = Request.Scheme + "//" + Request.Host + imagePath;

            if (media.Data != null)
            {
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
            if (!int.TryParse(postId, out int id) || id < 1)
                throw new InvalidOperationException("Invalid PostId. Please make sure you provide an Id of an existing post.");
            
            var post = PostBusiness.Load(id);
            if (post == null)
                throw new ArgumentException("Unable to retrieve Post: " + UserBusiness.ErrorMessage);

            var blogPost = new WeblogPost()
            {
                BlogId = "1", // only one blog so we hardcode this
                PostId = post.Id.ToString(),
                Abstract = post.Abstract,
                Body = post.Body,
                RawPostText = post.Markdown,
                DateCreated = post.Created,
                Url = post.Url,
                PermaLink = post.Url               
            };

            if (!string.IsNullOrEmpty(post.Categories))
                blogPost.Categories = post.Categories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            return blogPost;
        }

        [HttpPost]
        [Route("list")]
        public override IList<WeblogMinimalPost> GetPosts([FromBody] PostListFilter listFilter)
        {            
            var posts = PostBusiness.GetLastPostsSync(listFilter.NumberOfPosts);

            var postList = new List<WeblogMinimalPost>();
            foreach (var post in posts)
            {
                postList.Add(item: new WeblogMinimalPost()
                {
                    PostId = post.Id.ToString(),
                    Title = post.Title,
                    Abstract = post.Abstract,
                    Created = post.Created,
                    Url = post.Url,
                    ImageUrl = post.ImageUrl,
                    CommentCount = post.CommentCount,
                });
            }
            
            return postList;
        }
        
    }



    
}
