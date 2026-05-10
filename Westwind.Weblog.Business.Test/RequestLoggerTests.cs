using NUnit.Framework;
using System;
using Westwind.Weblog.Business.Utilities;

namespace Westwind.Weblog.Business.Test
{
    [TestFixture]
    public class RequestLoggerTests
    {
        public string ConnectionString = "server=.;database=WeblogCore;integrated security=true;encrypt=false";

        [Test]
        public void RequestLoggerTest()
        {

            DbResult<bool> res = RequestLogger.EnsureTablesExist();
            Console.WriteLine(res.Message);

            res = RequestLogger.LogRequest("testPostId", "testReferrer", "127.0.0.3");
            Console.WriteLine(res.Message);
            res = RequestLogger.LogRequest("testPostId2", "testReferrer2", "127.0.0.1");
            Console.WriteLine(res.Message);
            res = RequestLogger.LogRequest("testPostId", "testReferrer2", "127.0.0.1");
            Console.WriteLine(res.Message);
            res = RequestLogger.LogRequest("testPostId2", "testReferrer2", "127.0.0.2");
            Console.WriteLine(res.Message);
        }

        [Test]
        public void RequestLoggerClearTest()
        {

            DbResult<bool> res = RequestLogger.EnsureTablesExist();
            Console.WriteLine(res.Message);

            res = RequestLogger.ClearRequests(2);

            Assert.IsTrue(res, "Failed to clear requests: " + res.Message);
        }


    }
}
