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

        public StringBuilder DeleteOldImages(string imageFolder)
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

                // only /image folders that start with a number
                if (char.IsDigit(dirName[0]))
                    DeleteOldImagesInFolder(dir, sb, postList);
            }

            return sb;
        }

        public void DeleteOldImagesInFolder(string imagePath, StringBuilder sb, string postList)
        {
            foreach (var dir in Directory.GetDirectories(imagePath))
            {
                DeleteOldImagesInFolder(dir, sb, postList);
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


        #region Statistics

        public List<PostHitResult> PostHits(
            DateTime start = default,
            DateTime end = default,
            int maxRows = 10)
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
                    having Count(*) > 1
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

        public class PostHitResult
        {
            // implement
            public string Title { get; set; }
            public string Url { get; set; } 

            public int Hits { get; set; }
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

            foreach (var item in data)
            {
                item.TargetUrl = wlApp.Configuration.ApplicationBasePath.TrimEnd('/') + item.TargetUrl;
            }

            return data;
        }


        #endregion

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
