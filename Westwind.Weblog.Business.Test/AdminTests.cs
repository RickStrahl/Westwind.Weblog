using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
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
        public string ConnectionString = "server=.;database=WeblogCore;integrated security=true;encrypt=false";
        public string OldConnectionString = "server=.;database=Weblog;integrated security=true;encrypt=false";

        [Test]
        public void DeleteOldImages()
        {
            var context = GetContext();
            var repo = new AdminBusiness(context, new Configuration.WeblogConfiguration());

            repo.DeleteOldImages(@"d:\projects\Westwind.Weblog\Westwind.Weblog\wwwroot\images");
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

            Assert.IsTrue(WeblogDataImporter.EnsureWeblogData(ctx, "server=.;database=weblog;integrated security=true;encrypt=false;"), "Failed to import weblog data");
        }

        [Test]
        public void CreateDatabaseTest()
        {
            GetContext();
        }

        [Test]
        public void DeletePostsAndCommentsTest()
        {
            //var context = GetContext();
            //int result = context.Database.ExecuteSql($"delete from Comments;delete from Posts");                        
        }


        [Test]
        public void RequestLoggerTest()
        {

            DbResult<bool> res = RequestLogger.EnsureTablesExist();
            Console.WriteLine(res.Message);

            res = RequestLogger.LogRequest("testPostId", "testReferrer", "127.0.0.3");
            Console.WriteLine(res.Message);
            res = RequestLogger.LogRequest("testPostId2", "testReferrer2", "127.0.0.1");
            res = RequestLogger.LogRequest("testPostId", "testReferrer2", "127.0.0.1");
            res = RequestLogger.LogRequest("testPostId2", "testReferrer2", "127.0.0.2");
        }

        [Test]
        public void RequestLoggerClearTest()
        {

            DbResult<bool> res = RequestLogger.EnsureTablesExist();
            Console.WriteLine(res.Message);

            res = RequestLogger.ClearRequests(2);

            Assert.IsTrue(res, "Failed to clear requests: " + res.Message);
        }


        WeblogContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(ConnectionString)
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
