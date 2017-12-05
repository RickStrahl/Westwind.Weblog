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
        public static bool EnsureWeblogData(WeblogContext context)
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
                hasData = ImportFromExistingDb(context) > 0;                
            }

            if (!hasData)
                throw new InvalidOperationException("No data found and no data created...");



            return true;
        }

        /// <summary>
        /// Imports data from json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static int ImportFromExistingDb(WeblogContext context)
        {
            var sql = new SqlDataAccess("server=.;database=weblog;integrated security=true");
            var data = sql.ExecuteTable("weblogposts", "select * from blog_entries where EntryType=1");

            //var sql2 = new SqlDataAccess("server=.;database=weblogCore;integrated security=true");


            //var conn = context.Database.GetDbConnection();
            //if (conn.State != ConnectionState.Open)
            //    conn.Open();

            var count = 0;

            // use transaction to force connection open and keep open for IDENTITY INSERT to work
            using (var tx = context.Database.BeginTransaction())
            {
                context.Database.ExecuteSqlCommand("SET IDENTITY_INSERT Posts ON");
   
                foreach (DataRow row in data.Rows)
                {
                    var pk = (int) row["pk"];

                    var post = new Post();

                    DataUtils.CopyObjectFromDataRow(row, post);

                    post.Id = pk;

                    context.Posts.Add(post);

                    
                    context.SaveChanges();
                    //context.Database.ExecuteSqlCommand("SET IDENTITY_INSERT dbo.Weblogs ON; " +
                    //                                   "update weblogs set id={0} where id={1};" +
                    //                                   "SET IDENTITY_INSERT dbo.Weblogs OFF;", pk, post.Id); 
                    
                }
                
                context.Database.ExecuteSqlCommand("SET IDENTITY_INSERT Posts OFF");
                tx.Commit();
            }

            
            data = sql.ExecuteTable("weblogcomments", "select * from blog_entries where EntryType=3");

            count = 0;
            using (var tx = context.Database.BeginTransaction())
            {
                context.Database.ExecuteSqlCommand("SET IDENTITY_INSERT Comments ON");

                foreach (DataRow row in data.Rows)
                {
                    var pk = (int)row["pk"];
                    var postPk = (int) row["ParentPk"];
                    if (postPk < 1)
                        continue;

                    if (!context.Posts.Any(p => p.Id == postPk)) 
                        continue;

                    var comment = new Comment();                    

                    DataUtils.CopyObjectFromDataRow(row, comment);

                    comment.Id = pk;
                    comment.PostId = postPk;

                    context.Comments.Add(comment);

                    // save on every 20th record to avoid 
                    // change tracking to overload
                    if (count % 20 == 0)
                        context.SaveChanges();
                    
                    count++;
                }

                // save remainder
                context.SaveChanges();

                context.Database.ExecuteSqlCommand("SET IDENTITY_INSERT Comments OFF");
                tx.Commit();
            }

            return count;
        }
    }
}