using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    public class AdminBusiness : EntityFrameworkBusinessObject<WeblogContext,Post>
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

            foreach (var file in Directory.GetFiles(imagePath))
            {
                var ext = Path.GetExtension(file);
                if (ext != null)
                    ext = ext.ToLower();
                if (ext != ".png" && ext != ".gif" && ext != "jpg" && ext != "jpeg")
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
    }
}
