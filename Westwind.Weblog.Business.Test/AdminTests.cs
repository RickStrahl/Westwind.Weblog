using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business.Test
{
    [TestFixture]
    public class AdminTests
    {
        public string ConnectionString = "server=.;database=WeblogCore;integrated security=true;";

        [Test]
        public void DeleteOldImages()
        {
            var context = GetContext();
            var repo = new AdminRepository(context, new Configuration.WeblogConfiguration());

            repo.DeleteOldImages(@"C:\projects2010\Westwind.Weblog\Westwind.Weblog\wwwroot\images");
        }


        WeblogContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            var ctx = new WeblogContext(options);

            WeblogDataImporter.EnsureWeblogData(ctx, "server=.;database=Weblog;integrated security=true;");
            return ctx;

        }
    }
}
