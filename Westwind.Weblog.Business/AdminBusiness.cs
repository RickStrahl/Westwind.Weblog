using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.Data.EfCore;
using Westwind.Utilities;
using Westwind.Utilities.Data;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business
{
    public class AdminBusiness : EntityFrameworkBusinessObject<WeblogContext, Post>
    {
        new WeblogContext Context { get; set; }
        readonly WeblogConfiguration WeblogConfiguration;


        public AdminBusiness(WeblogContext context,
                              WeblogConfiguration config) : base(context)
        {
            WeblogConfiguration = config;
            Context = context;
        }

        public bool ImportOldWebLog(string oldWeblogConnectionString)
        {
            var connStr = Context.ConnectionString;
            var sql = new SqlDataAccess(connStr);
            int res = sql.ExecuteNonQuery("drop table Comments");
            res = sql.ExecuteNonQuery("drop table Posts");
            res = sql.ExecuteNonQuery("drop table Users");
            res = sql.ExecuteNonQuery("drop table Weblogs");

            //if (res < 0)
            //{
            //    SetError(sql.ErrorMessage);
            //    return false;
            //}

            return WeblogDataImporter.EnsureWeblogData(Context, wlApp.Configuration.OldWeblogConnectionString);
        }

        public StringBuilder DeleteUnusedImages(string imageFolder)
        {
            StringBuilder sb = new StringBuilder();

            var posts = Context.Posts.Select(p => new Post
            {
                Id = p.Id,
                Body = p.Body,
                FeaturedImageUrl = p.FeaturedImageUrl
            });

            StringBuilder sbContent = new StringBuilder(500000);
            foreach (var post in posts)
            {
                sbContent.Append(post.Body + "\r\n" + post.FeaturedImageUrl);
            }

            string postList = sbContent.ToString().ToLower();
            sbContent.Clear();            

            foreach (var dir in Directory.GetDirectories(imageFolder))
            {
                var dirName = Path.GetFileName(dir);

                // only folders that start with a number
                if (char.IsDigit(dirName[0]))
                    DeleteUnusedImagesInFolder(dir, sb, postList);
            }

            return sb;
        }

        public void DeleteUnusedImagesInFolder(string imagePath, StringBuilder sb, string postList)
        {
            foreach (var dir in Directory.GetDirectories(imagePath))
            {
                DeleteUnusedImagesInFolder(dir, sb, postList);
            }

            var files = Directory.GetFiles(imagePath);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext != null)
                    ext = ext.ToLower();
                if (ext != ".png" && ext != ".gif" && ext != ".jpg" && ext != ".jpeg")
                    continue;

                var filename = Path.GetFileName(file);
                var lFile = filename.ToLower();
                var ueFile = StringUtils.UrlEncode(lFile);

                if (postList.Contains(lFile) || postList.Contains(ueFile))
                    continue;

                try
                {
                    File.Delete(file);
                    sb.AppendLine(file);
                    Debug.WriteLine(file);
                }
                catch
                {
                }
            }
        }

        public bool UpdatePostCommentCounts()
        {

            foreach (var post in Context.Posts)
            {
                var commentCount = Context.Comments.Count(c => c.PostId == post.Id);

                if (commentCount != post.CommentCount)
                {
                    var sql = $"update Posts set CommentCount = @1 where Id =@0";
                    var result = Db.ExecuteNonQuery(sql, post.Id, commentCount);

                    //Context.Posts.Attach(post);
                    //post.CommentCount = commentCount;
                    //Context.SaveChanges();
                }
            }

            return true;
        }

        public bool ShrinkDatabase(string databaseName = null)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                var builder = new SqlConnectionStringBuilder(wlApp.Configuration.ConnectionString);
                databaseName = builder.InitialCatalog;
            }
            if (string.IsNullOrEmpty(databaseName))
                databaseName = "WeblogCore";

            string sql = $@"DBCC SHRINKDATABASE('{databaseName}');";
            if (Db.ExecuteNonQuery(sql) < 0)
            {
                SetError(Db.ErrorMessage);
                return false;
            }

            return true;
        }

        public async Task<List<PostListItem>> GetRecentPostsForEditorAsync(string search = null, int count = 100)
        {
            List<PostListItem> posts = [];

            var postBus = new PostBusiness(Context, WeblogConfiguration);
            if (!string.IsNullOrWhiteSpace(search))
            {
                
                posts = await postBus.PostSearchAsync(search, 20, includeInactive: true);

                //search = search.Trim().ToLower();
                //posts = posts.Where(p =>
                //    p.Title.ToLower().Contains(search) ||
                //    p.SafeTitle.ToLower().Contains(search) ||
                //    p.Id.ToLower().Contains(search) ||
                //    (p.Categories != null && p.Categories.ToLower().Contains(search)) ||
                //    (p.Keywords != null && p.Keywords.ToLower().Contains(search)) ||
                //    (p.Abstract != null && p.Abstract.ToLower().Contains(search)));
            }
            else
            {
                posts = await postBus.PostSearchAsync(search, 100, includeInactive: true);
            }

            return posts;                
        }

        public Post LoadPostForEditor(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                return null;

            return Context.Posts
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == postId);
        }

        public Post CreateNewPostForEditor()
        {
            var lastPost = Context.Posts
                .OrderByDescending(p => p.Created)
                .FirstOrDefault();

            return new Post
            {
                BlogId = lastPost?.BlogId ?? Context.Weblogs.Select(w => w.Id).FirstOrDefault(),
                Author = WeblogConfiguration.WeblogAuthor,
                BodyMode = 2,
                Active = true,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                Location = lastPost?.Location
            };
        }

        public bool SavePostForEditor(string originalPostId, Post editedPost)
        {
            if (editedPost == null)
            {
                SetError("No post was submitted.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(editedPost.Title))
            {
                SetError("Title is required.");
                return false;
            }

            var isNewPost = string.IsNullOrWhiteSpace(originalPostId);
            var effectivePostId = string.IsNullOrWhiteSpace(editedPost.Id)
                ? wlApp.NewId()
                : editedPost.Id.Trim();

            if (isNewPost && Context.Posts.Any(p => p.Id == effectivePostId))
            {
                SetError("A post with the specified Id already exists.");
                return false;
            }

            if (!isNewPost && !string.Equals(originalPostId, effectivePostId, StringComparison.OrdinalIgnoreCase))
            {
                if (Context.Posts.Any(p => p.Id == effectivePostId))
                {
                    SetError("A post with the specified Id already exists.");
                    return false;
                }

                var renameResult = Db.ExecuteNonQuery(
                    """
                    update Comments set PostId = @0 where PostId = @1;
                    update PostHits set PostId = @0 where PostId = @1;
                    update Posts set Id = @0 where Id = @1;
                    """,
                    effectivePostId,
                    originalPostId);

                if (renameResult < 0)
                {
                    SetError(Db.ErrorMessage);
                    return false;
                }

                Context.ChangeTracker.Clear();
                originalPostId = effectivePostId;
            }

            var existingPost = isNewPost
                ? null
                : Context.Posts.FirstOrDefault(p => p.Id == originalPostId);

            var post = existingPost ?? new Post();
            post.Id = effectivePostId;
            post.BlogId = editedPost.BlogId;
            post.Title = editedPost.Title?.Trim();
            post.Body = editedPost.Body;
            post.Abstract = editedPost.Abstract;
            post.Created = editedPost.Created.Year > 2000 ? editedPost.Created : (existingPost?.Created ?? DateTime.Now);
            post.Updated = editedPost.Updated.Year > 2000 ? editedPost.Updated : DateTime.Now;
            post.Active = editedPost.Active;
            post.Author = editedPost.Author;
            post.BodyMode = editedPost.BodyMode;
            post.CommentCount = editedPost.CommentCount;
            post.CommentsClosed = editedPost.CommentsClosed;
            post.Categories = editedPost.Categories;
            post.Keywords = editedPost.Keywords;
            post.Location = editedPost.Location;
            post.RedirectUrl = editedPost.RedirectUrl;
            post.SafeTitle = string.IsNullOrWhiteSpace(editedPost.SafeTitle)
                ? PostBusiness.GetSafeTitleStatic(editedPost.Title)
                : editedPost.SafeTitle.Trim();
            post.IsFeatured = editedPost.IsFeatured;
            post.Markdown = editedPost.Markdown;
            post.PermanentUrl = editedPost.PermanentUrl;
            post.FeaturedImageUrl = editedPost.FeaturedImageUrl;
            post.GithubUrl = editedPost.GithubUrl;
            post.IsArticle = editedPost.IsArticle;
            post.Hits = editedPost.Hits;

            if (string.IsNullOrWhiteSpace(post.PermanentUrl))
                post.PermanentUrl = post.GetPostUrl();

            if (existingPost == null)
                Context.Posts.Add(post);

            try
            {
                Context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex);
                return false;
            }
        }

        public bool DeletePostForEditor(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
            {
                SetError("No post was selected.");
                return false;
            }

            var post = Context.Posts.FirstOrDefault(p => p.Id == postId);
            if (post == null)
            {
                SetError("Post not found.");
                return false;
            }

            try
            {
                var comments = Context.Comments.Where(c => c.PostId == postId).ToList();
                if (comments.Count > 0)
                    Context.Comments.RemoveRange(comments);

                if (Db.ExecuteNonQuery("delete from PostHits where PostId = @0", postId) < 0)
                {
                    SetError(Db.ErrorMessage);
                    return false;
                }

                Context.Posts.Remove(post);
                Context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex);
                return false;
            }
        }


        #region Statistics

        public List<PostHitResult> PostHits(
            DateTime start = default,
            DateTime end = default,
            int maxRows = 50)
        {

            if (end == default)
                end = DateTime.Now.Date.AddDays(1);
            if (start == default)
                start = DateTime.Now.Date.AddDays(-7);

            var sql =
                $"""
                select top {maxRows} posts.Title,
                CONCAT(
                    '/posts/',
                    DATEPART(year, posts.Created), '/',
                    DATEName(month , posts.Created), '/',
                    DATEPART(day, posts.Created), '/',
                    posts.SafeTitle
                    ) AS Url,
                        count(*) as Hits
                    from Posts, PostHits
                    where Posts.Id = PostHits.PostId AND CAST(Timestamp AS DATE) BETWEEN @StartDate AND @EndDate
                    Group by posts.Title, CONCAT(
                    '/posts/',
                    DATEPART(year, posts.Created), '/',
                    DATEName(Month, posts.Created), '/',
                    DATEPART(day, posts.Created), '/',
                    posts.SafeTitle
                    )                    
                    order by Hits Desc
                """;

            var data = Db.QueryList<PostHitResult>(sql,
                Db.CreateParameter("@StartDate", start),
                Db.CreateParameter("@EndDate", end));
            if (data == null)
            {
                SetError(Db.ErrorMessage);
                return null;
            }

            foreach(var item in data)
            {
                item.Url = wlApp.Configuration.ApplicationBasePath.TrimEnd('/') + item.Url;
            }

            return data;
        }

        public int DeletePostHitsOlderThan(int days)
        {
            var cutoffDate = DateTime.Now.Date.AddDays(-days);
            var sql = "delete from PostHits where CAST(Timestamp as DATE) < @CutoffDate";

            var result = Db.ExecuteNonQuery(sql,
                Db.CreateParameter("@CutoffDate", cutoffDate));

            if (result < 0)
            {
                SetError(Db.ErrorMessage);
                return -1;
            }

            return result;
        }


        public List<ReferrerResult> Referrers()
        {
            var sql =
                """
                --Hit counts per referrer across all posts
                select p.id as PostId,
                count(*) as HitCount,
                p.Title,
                CONCAT(
                    '/posts/',
                    DATEPART(year, p.Created), '/',
                    DATENAME(month, p.Created), '/',
                    DATEPART(day, p.Created), '/',
                    p.SafeTitle
                    ) as TargetUrl,
                ph.Referrer as Referrer
                    from posts p
                inner join postHits ph on p.id = ph.postId
                    group by p.id, p.Title,
                    CONCAT(
                        '/posts/',
                        DATEPART(year, p.Created), '/',
                        DATENAME(month, p.Created), '/',
                        DATEPART(day, p.Created), '/',
                        p.SafeTitle
                    ),
                    ph.Referrer
                    having count(*) > 1
                order by HitCount desc, p.id, ph.Referrer
                """;

            var data = Db.QueryList<ReferrerResult>(sql);
            if (data == null)
            {
                SetError(Db.ErrorMessage);
                return null;
            }

            data = data.Where(l =>
            {
                if (string.IsNullOrEmpty(l.Referrer)) return false;

                if (
                    l.Referrer.Contains("google.com/") ||
                        l.Referrer.Contains("duckduckgo.com/") ||
                        l.Referrer.Contains("bing.com/") ||
                        l.Referrer.Contains("/weblog.west-wind.com/")
                    )
                    return false;
                l.TargetUrl = wlApp.Configuration.ApplicationBasePath.TrimEnd('/') +l.TargetUrl;
                return true;
            }).ToList();

            return data;
        }

        #endregion

    }



    public class PostHitResult
    {
        // implement
        public string Title { get; set; }
        public string Url { get; set; }

        public int Hits { get; set; }
    }

    public class ReferrerResult
    {
        public string PostId { get; set; }
        public int HitCount { get; set; }
        public string Title { get; set; }
        public string TargetUrl { get; set; }
        public string Referrer { get; set; }
    }

}
