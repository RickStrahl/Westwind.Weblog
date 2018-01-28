using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.Data.EfCore;
using Westwind.Utilities;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business
{
    public class PostBusiness : EntityFrameworkBusinessObject<WeblogContext,Post>
    {
        public readonly WeblogConfiguration WeblogConfiguration;
        

        public PostBusiness(WeblogContext context,  
                              WeblogConfiguration config) : base(context)
        {
            WeblogConfiguration = config;            
        }

        #region Post Retrieval
        public async Task<List<Post>> GetLastPosts(int postCount = 50)
        {            
            return await Context.Posts
                .Include("Comments")
                .OrderByDescending(p => p.Entered)
                .Take(postCount)
                .Select( p=> new Post
                {
                   Id = p.Id,
                   IsFeatured = p.IsFeatured,
                   Abstract = p.Abstract,
                   Title = p.Title,
                   SafeTitle = p.SafeTitle,
                   Location = p.Location,
                   CommentCount  = p.CommentCount,
                   Entered = p.Entered
                })
                .ToListAsync();
        }

        public async Task<List<Comment>> GetRecentComments(int postCount = 30)
        {
            return await Context.Comments
                .OrderByDescending(c => c.Entered)
                .Take(postCount)
                .Select(c => new Comment
                {
                    Id = c.Id,
                    Title = c.Title,
                    Body = c.Body,
                    BodyMode = c.BodyMode,
                    Author = c.Author,
                    Url = c.Url,
                    Email = c.Email,
                    Entered = c.Entered,
                    PostId = c.PostId
                }).ToListAsync();
        }


        /// <summary>
        /// Retrieves a post by its title slug
        /// </summary>
        /// <param name="slug">Post title created with GetSlug() and held in SafeTitle</param>
        /// <returns></returns>
        public async Task<Post> GetPost(string slug)
        {            
            Entity = await Context.Posts
                .Include("Comments")            
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SafeTitle == slug);
            return Entity;
        }

        


        /// <summary>
        /// Retrieves a post by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Post> GetPost(int id)
        {
            Entity = await Context.Posts
                    .Include("Comments")                    
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
            return Entity;
        }
        #endregion 

        #region Comments
        /// <summary>
        /// Explicitly lazy loads comments for a post.
        /// </summary>
        /// <param name="post"></param>
        public async void LoadComments(Post post = null)
        {
            if (post == null)
                post = Entity;

            post.Comments = await Context.Comments
                                .Where(c => c.PostId == post.Id)
                                .ToListAsync();
        }

        /// <summary>
        /// Returns a string of x comments,1 comment or blank if there are no comments
        /// </summary>
        /// <param name="post">post instance</param>
        /// <returns></returns>
        public string ShowCommentCount(Post post = null)
        {
            if (post == null)
                post = Entity;

            if (post.CommentCount == 0)
                return string.Empty;

            string commentCountText;

            if (post.CommentCount == 1)
                commentCountText = "1 comment";
            else
                commentCountText = post.CommentCount + " comments";

            return commentCountText;
               
        }
        #endregion


        #region Url Processing

        /// <summary>
        /// Returns the full URL to this entry entity.
        /// </summary>
        /// <returns></returns>
        public string GetPostUrl(Post post = null)
        {
            if (post == null)
                post = Entity;
            if (post == null) return null;

            if (!string.IsNullOrEmpty(post.RedirectUrl))
                return post.RedirectUrl;

            return GetPostUrl(post.SafeTitle, post.Entered);
        }

        /// <summary>
        /// Returns a POST URL from a safe Title and entered date
        /// </summary>
        /// <param name="entered"></param>
        /// <param name="safeTitle"></param>
        /// <returns></returns>
        public string GetPostUrl(string safeTitle, DateTime entered, bool force = false)
        {
            DateTime date = entered;
            string url = $"{WeblogConfiguration.ApplicationBasePath}posts/{date.Year}/{date:MMM}/{date:dd}/{safeTitle}";                         
            return url;
        }

        /// <summary>
        /// Returns a URL safe string for the title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public string GetSlug(string title = null)
        {
            if (title == null)
                title = Entity.Title;

            title = WebUtility.HtmlDecode(title);

            StringBuilder sb = new StringBuilder();

            foreach (char ch in title)
            {
                if (ch == 32)
                    sb.Append("-");
                else if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
            }

            sb.Replace("---", "-");
            sb.Replace("--", "-");

            return sb.ToString();
        }
        #endregion

        /// <summary>
        /// Returns post stats
        /// </summary>
        /// <returns>postCount, commentCount tuple</returns>
        public (int postCount, int commentCount) GetPostStats()
        {
            int postCount = Context.Posts.Count(p => p.Active);
            int commentCount = Context.Comments.Count();

            return (postCount, commentCount);
        }
    }
}
