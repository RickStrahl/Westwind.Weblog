using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Text;
using Westwind.Utilities.Data;
using Westwind.Weblog.Business.Models;
using Westwind.Weblog.Business.Utilities;

namespace Westwind.Weblog.Business.Test
{
    [TestFixture]
    public class AdminTests
    {
        public string ConnectionString = "server=west-wind.com;database=WeblogCore-MarkdownMonster;integrated security=true;encrypt=false";
        //public string ConnectionString = "server=west-wind.com;database=WeblogCore-WebConnection;integrated security=true;encrypt=false";
        public string OldConnectionString = "";// "server=.;database=Weblog;integrated security=true;encrypt=false";

        [Test]
        public void DeleteOldImages()
        {
            var context = GetContext();
            var repo = new AdminBusiness(context, new Configuration.WeblogConfiguration());

            repo.DeleteUnusedImages(@"d:\projects\Westwind.Weblog\Westwind.Weblog\wwwroot\images");
        }
        
        [Test]
        public void UpdatePostCounts()
        {
            var repo = GetAdminRepo();
            repo.UpdatePostCommentCounts();
        }


        [Test]
        public void ImportOldWebLogTest()
        {
            
            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            var ctx = new WeblogContext(options);

            Assert.IsNotNull(ctx, "Failed to create WeblogContext");

            var sql = new SqlDataAccess(ConnectionString);
            int res = sql.ExecuteNonQuery("drop table Comments");
            res = sql.ExecuteNonQuery("drop table Posts");
            res = sql.ExecuteNonQuery("drop table Users");
            res = sql.ExecuteNonQuery("drop table Weblogs");

            //if (res < 0)
            //{
            //    SetError(sql.ErrorMessage);
            //    return false;
            //}

            Assert.IsTrue(WeblogDataImporter.EnsureWeblogData(ctx, OldConnectionString), "Failed to import weblog data");
        }

        [Test]
        public void CreateDatabaseTest()
        {
            GetContext();
        }


        [Test]
        public void ImportFromWebConnectionBlogTest()
        {
            var cs = "server=west-wind.com;database=WeblogCore-WebConnection;integrated security=true;encrypt=false";
            var ctx = GetContext(cs);
            Console.WriteLine(ctx.Database.GetConnectionString());
            
            int result = ctx.Database.ExecuteSql($"delete from Comments;delete from Posts");   
            int count = WeblogDataImporter.ImportFromWebConnectionOleDb(ctx);
            ClassicAssert.IsTrue(count > 0, "No records imported");
        }




        [Test]
        public void DeletePostsAndCommentsTest()
        {
            //var context = GetContext();
            //int result = context.Database.ExecuteSql($"delete from Comments;delete from Posts");                        
        }

        WeblogContext GetContext(string connectionString=null)
        {
            if (string.IsNullOrEmpty(connectionString))
                connectionString = ConnectionString;

            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(connectionString) 
                .Options;

            var ctx = new WeblogContext(options);

            WeblogDataImporter.EnsureWeblogData(ctx, OldConnectionString);
            return ctx;
        }

        AdminBusiness GetAdminRepo()
        {
            var context = GetContext();
            return new AdminBusiness(context, new Configuration.WeblogConfiguration());
        }


    }
}
