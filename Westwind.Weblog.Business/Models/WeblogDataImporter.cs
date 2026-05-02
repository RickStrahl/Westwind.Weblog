using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.Utilities;
using Westwind.Utilities.Data;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business.Models
{

    /// <summary>
    /// This class imports Albums, artists and tracks from the
    /// wwwroot/data/albums.js file which contains all the data
    /// in a single graph.
    /// </summary>
    public  class WeblogDataImporter
    {
        /// <summary>
        /// Ensures that the database and table structure exists
        /// </summary>
        /// <param name="context"></param>
        /// <param name="oldConnectionString"></param>
        /// <returns></returns>
        public static bool EnsureWeblogData(WeblogContext context,string oldConnectionString)
        {
            bool hasData = false;
            try
            {
                hasData = context.Posts.Any();
            }
            catch
            { }

            if (!hasData)
            {
                context.Database.EnsureCreated(); // just create the schema - no migrations
                hasData = ImportFromExistingDb(context,oldConnectionString) > 0;                
            }

            if (!hasData)
                throw new InvalidOperationException("No data found and no data created...");

            return true;
        }

        /// <summary>
        /// Imports data from existing Weblog database
        /// </summary>
        /// <param name="context">The context to work with</param>
        /// <param name="oldConnectionString">The old connection string of the DB to import from</param>
        /// <returns></returns>
        public static int ImportFromExistingDb(WeblogContext context, string oldConnectionString)
        {

            var sql = new SqlDataAccess(oldConnectionString);
            var data = sql.ExecuteTable("weblogposts", "select * from blog_entries where EntryType=1");

            var conn = context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            var count = 0;


            foreach (DataRow row in data.Rows)
            {
                var pk = (int) row["pk"];

                var post = new Post();

                DataUtils.CopyObjectFromDataRow(row, post);

                post.Id = pk.ToString();                
                
                post.Created = (DateTime) row["Entered"];                
                post.CommentCount = (int) row["Feedback"];
                post.FeaturedImageUrl = row["FeaturedImageUrl"] as string; 
                
                context.Posts.Add(post);

                // save on every 10th record to avoid 
                // change tracking to overload
                if (count % 10 == 0)
                    context.SaveChanges();

                count++;
            }
            context.SaveChanges();

            
            data = sql.ExecuteTable("weblogcomments", "select * from blog_entries where EntryType=3");

            count = 0;

            foreach (DataRow row in data.Rows)
            {
                var pk = (int) row["pk"];
                var postPk = (int) row["ParentPk"];
                if (postPk < 1)
                    continue;

                // skip if post doesn't exist
                if (!context.Posts.Any(p => p.Id == postPk.ToString()))
                    continue;

                var comment = new Comment();

                DataUtils.CopyObjectFromDataRow(row, comment);

                comment.Id = pk.ToString();
                comment.PostId = postPk.ToString();
                comment.Created = (DateTime)row["Entered"];
                comment.BodyMode = (int) row["BodyMode"];


                comment.IsActive = row["Active"] as bool? ?? true;

                context.Comments.Add(comment);

                // save on every 20th record to avoid 
                // change tracking to overload
                if (count % 10 == 0)
                    context.SaveChanges();

                count++;
            }

            // save remainder
            context.SaveChanges();

            
            var user = new User()
            {
                Fullname = "Rick Strahl",      
                Password = "testing",
                Username = "rstrahl@west-wind.com",
                IsAdmin = true
            };
            context.Users.Add(user);
            context.SaveChanges();

            user.Password = "testing"; // add again to force encryption with ID
            context.SaveChanges();

            return count;
        }
    }
}