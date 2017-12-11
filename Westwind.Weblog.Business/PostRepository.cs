using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.Data.EfCore;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business
{
    public class PostRepository : EntityFrameworkRepository<WeblogContext,Post>
    {
        public readonly WeblogConfiguration WeblogConfiguration;
        

        public PostRepository(WeblogContext context, 
                              WeblogConfiguration config) : base(context)
        {
            WeblogConfiguration = config;            
        }


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

        public async Task<Post> GetPost(string slug)
        {            
            return await Context.Posts
                .Include("Comments")            
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SafeTitle == slug);
        }

        public async Task<Post> GetPost(int id)
        {
            return await Context.Posts
                    .Include("Comments")                    
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
        }


        #region Url Processing

        /// <summary>
        /// Returns the full URL to this entry entity.
        /// </summary>
        /// <returns></returns>
        public string GetPostUrl(Post post)
        {
            if (post == null)
                return null;

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
        /// Returns a string of x comments,1 comment or blank if there are no comments
        /// </summary>
        /// <param name="feedBackCount"></param>
        /// <param name="entryPk"></param>
        /// <returns></returns>
        public string ShowCommentCount(Post post)
        {
            if (post.CommentCount == 0)
                return "";

            string commentCountText;

            if (post.CommentCount == 1)
                commentCountText = "1 comment";
            else
                commentCountText = post.CommentCount + " comments";

            return commentCountText;
            var postUrl = GetPostUrl(post) + "#Feedback";
            return 

$@"<a class='hoverbutton' href='{WeblogConfiguration.ApplicationBasePath}posts/{post.Id}.aspx#Feedback'>
    <img src='{WeblogConfiguration.ApplicationBasePath}images/comment.gif' />{commentCountText}
</a>";

            return commentCountText;         
        }
        #endregion
    }
}
