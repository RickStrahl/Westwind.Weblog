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
    public class PostBusiness : EntityFrameworkBusinessObject<WeblogContext, Post>
    {
        public readonly WeblogConfiguration Configuration;


        public PostBusiness(WeblogContext context,
                            WeblogConfiguration config) : base(context)
        {
            Configuration = config;
        }

        #region Post Retrieval

        public async Task<List<Post>> GetLastPostsAsync(int postCount = 75, bool includeBody = false)
        {
            return await Context.Posts
                .Where(p => p.Active)
                .Include("Comments")                
                .OrderByDescending(p => p.Created)
                .Take(postCount)
                .Select(p => new Post
                {
                    Id = p.Id,
                    IsFeatured = p.IsFeatured,
                    Abstract = p.Abstract,
                    Title = p.Title,
                    SafeTitle = p.SafeTitle,
                    Location = p.Location,
                    CommentCount = p.CommentCount,
                    Created = p.Created,
                    Body = includeBody ? p.Body : null,
                    Active = p.Active,
                    FeaturedImageUrl = p.FeaturedImageUrl
                })
                .ToListAsync();
        }

        public List<Post> GetLastPosts(int postCount = 75, bool includeBody = false)
        {
            return Context.Posts
                .Where(p => p.Active)
                .Include("Comments")
                .OrderByDescending(p => p.Created)
                .Take(postCount)
                .Select(p => new Post
                {
                    Id = p.Id,
                    IsFeatured = p.IsFeatured,
                    Abstract = p.Abstract,
                    Title = p.Title,
                    SafeTitle = p.SafeTitle,
                    Location = p.Location,
                    CommentCount = p.CommentCount,
                    Created = p.Created,
                    Body = includeBody ? p.Body : null,
                    FeaturedImageUrl = p.FeaturedImageUrl
                })
                .ToList();
        }


        public async Task<List<PostListItem>> PostSearchAsync(string postSearch, int postCount = 15)
        {
            postSearch = postSearch.ToLower();

            return await Context.Posts
                .Where(p=> p.Active && 
                           (p.Title.ToLower().Contains(postSearch) || 
                           p.Abstract.ToLower().Contains(postSearch)) )                
                .OrderByDescending(p => p.Created)
                .Take(postCount)
                .Select(p => new PostListItem
                {
                    PostId = p.Id,                                      
                    Abstract = p.Abstract,
                    Title = p.Title,
                    Url = p.GetPostUrl(),
                    Location = p.Location,
                    CommentCount = p.CommentCount,
                    Created = p.Created,                    
                    FeaturedImageUrl = p.FeaturedImageUrl
                })
                .ToListAsync();
        }
        

        public async Task<List<Comment>> GetRecentCommentsAsync(int commentCount = 50)
        {
            return await Context.Comments                
                .OrderByDescending(c => c.Created)
                .Join(Context.Posts, c => c.PostId,
                                     p => p.Id,
                                     (c, p) => new { Comment = c, Post = p })
                .Take(commentCount)
                .Select(c => new Comment
                {
                    Id = c.Comment.Id,
                    Title = c.Comment.Title,
                    Body = c.Comment.Body,
                    BodyMode = c.Comment.BodyMode,
                    Author = c.Comment.Author,
                    Url = c.Comment.Url,
                    Email = c.Comment.Email,
                    Created = c.Comment.Created,
                    PostId = c.Comment.PostId,
                    IsActive = c.Comment.IsActive,
                    Post = c.Post,
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
        ///  Loads the last post that was made
        /// </summary>
        public Post LoadLastPost()
        {
            Entity = Context.Posts.AsNoTracking()
                                  .OrderByDescending(p => p.Created)
                                  .FirstOrDefault();
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
        public string GetPostUrl(Post post = null, bool fullyQualified = false)
        {
            if (post == null)
                post = Entity;
            if (post == null) return null;

            if (!string.IsNullOrEmpty(post.RedirectUrl))
                return post.RedirectUrl;

            if (string.IsNullOrEmpty(post.SafeTitle))
                post.SafeTitle = GetSafeTitle(post.Title);

            return GetPostUrl(post.SafeTitle, post.Created, fullyQualified);
        }

        /// <summary>
        /// Returns a POST URL from a safe Title and entered date
        /// </summary>
        /// <param name="safeTitle">
        /// An encoded safe title that replaces spaces with -
        /// and all other punction by stripping
        /// Use GetSlug() to create a safetitle
        /// </param>
        /// <param name="entered">Created date of the post</param>
        /// <param name="fullyQualified">If true returns a full http(s) url</param>
        /// <returns></returns>
        public string GetPostUrl(string safeTitle, DateTime entered, bool fullyQualified = false)
        {
            DateTime date = entered;
            string url = $"{Configuration.ApplicationBasePath}posts/{date.Year}/{date:MMM}/{date:dd}/{safeTitle}";

            if (!fullyQualified)
                return url;

            return Configuration.WeblogHomeUrl + url;
        }


        /// <summary>
        /// Returns a URL safe string for the title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public string GetSafeTitle(string title = null)
        {
            if (title == null)
                title = Entity.Title;

            if (string.IsNullOrEmpty(title))
                return null;

            title = WebUtility.HtmlDecode(title);

            title = title
                .Replace("c#", "csharp")
                .Replace("C#", "csharp")
                .Replace(" .net", " dotnet")
                .Replace(" .NET", " Dotnet");

            StringBuilder sb = new StringBuilder();

            foreach (char ch in title.ToCharArray())
            {
                if (ch == 32)
                    sb.Append("-");
                else if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
                // everything else is stripped
            }

            //Fix multiple dashes
            sb.Replace("---", "-");
            sb.Replace("--", "-");

            return sb.ToString();
        }

        #endregion

        #region Stats
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
        #endregion


        /// <summary>
        /// Replaces the Ads in a post. Pass in either Html or Markdown
        /// and it replaces the `##AD##` placeholders.
        /// 
        /// It replaces the 1st item with a fixed ad and all others
        /// via shuffling from adsnew.xml
        /// </summary>
        /// <param name="postHtml">Html or Markdown to replace ##AD## values with</param>
        /// <returns>Html or Markdown with ads replaced</returns>
        public string EmbedAds(string postHtml)
        {
            if (string.IsNullOrEmpty(postHtml))
                return postHtml;


            var adMan = AdManager.Ads;

            var ad = adMan.GetFirstContentAd();
            var restAds = adMan.GetShuffledContentAds().ToList();
            if (!string.IsNullOrEmpty(ad))
            {
                postHtml = StringUtils.ReplaceStringInstance(postHtml, "##AD##", ad, 1, true); // no Ads at the moment exclusive
            }
            if (restAds is { Count: > 0 })
            {

                for (int i = 1; i < restAds.Count; i++)
                {
                    if (!postHtml.Contains("##AD##", StringComparison.Ordinal))
                        break;

                    ad = restAds[i];
                    postHtml = StringUtils.ReplaceStringInstance(postHtml, "##AD##", ad, 1, true);
                }
            }

            return postHtml;
        }

        /// <summary>
        /// Checks to see if a comment has basic values set.
        /// 
        /// Sets ValidationErrors if errors are found.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool ValidateComment(Comment comment)
        {
            if (string.IsNullOrEmpty(comment.Body) || (comment.Body.Length < 20))
            {
                ValidationErrors.Add("Body", "A reasonably long comment body is required.");
            }
            if (string.IsNullOrEmpty(comment.Author)) {
                ValidationErrors.Add("Author", "Comment author is required.");
            }
            if (string.IsNullOrEmpty(comment.Email) ||  !comment.Email.Contains('@') || !comment.Email.Contains('.')) {
                ValidationErrors.Add("Email", "A valid comment email is required.");
            }

            if (ValidationErrors.Count > 0)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Result from a Post Listing which includes only 
    /// a few fields from a post.
    /// </summary>
    public class PostListItem
    {
        /// <summary>
        /// The Id of the post
        /// </summary>
        public string PostId { get; set; }


        /// <summary>
        /// Title for the post
        /// </summary>
        public string Title { get; set; }


        /// <summary>
        /// The short text abstract for the post
        /// </summary>
        public string Abstract { get; set; }

        /// <summary>
        /// Optional - and not included unless explicitly asked for
        /// to reduce amount of data returned.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Date the post was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Optional location where the post was created
        /// if set
        /// </summary>
        public string Location { get; set; }


        /// <summary>
        /// The fully qualified Url to the post
        /// </summary>
        public string Url { get; set; }


        /// <summary>
        /// The Featured Image Url
        /// </summary>
        public string FeaturedImageUrl { get; set; }


        /// <summary>
        /// A small image Url
        /// </summary>
        public string ThumbnailUrl { get; set; }


        /// <summary>
        /// Number of comments for this post. This is optional and not included unless explicitly asked for
        /// in the filter
        /// </summary>
        public int CommentCount { get; set; }

    }
}
