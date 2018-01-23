using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Westwind.Weblog.Business.Models;
using System.Linq;
using System.Threading.Tasks;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business.Test
{
    [TestFixture]
    public class UserTests
    {
        public string ConnectionString = "server=.;database=WeblogCore;integrated security=true;";

        [Test]
        public void UpdateUserUser()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            var user = userBus.GetUserByEmail("rstrahl@west-wind.com");
            Assert.IsNotNull(user);

            user.Password = "testing";

            var updatedUser = userBus.SaveUser(user);
            Assert.IsNotNull(updatedUser, userBus.ErrorMessage);

            Console.WriteLine(updatedUser.Password);
        }


        [Test]
        public void AuthenticateUserTest()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            bool result = userBus.AuthenticateUser("rstrahl@west-wind.com", "testing");
            Assert.IsTrue(result,userBus.ErrorMessage);            
        }


        [Test]
        public void AuthenticateUserFailTest()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            bool result = userBus.AuthenticateUser("rstrahl@west-wind.com", "Bogus");
            Assert.IsFalse(result, userBus.ErrorMessage);

            result = userBus.AuthenticateUser("rstrahl@west-wind.com", "");
            Assert.IsFalse(result, userBus.ErrorMessage);

            result = userBus.AuthenticateUser("rstrahl@west-wind.com", null);
            Assert.IsFalse(result, userBus.ErrorMessage);

            result = userBus.AuthenticateUser(null,"testing");
            Assert.IsFalse(result, userBus.ErrorMessage);
        }

        [Test]
        public void AuthenticateAndRetrieveUserTest()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            string email = "rstrahl@west-wind.com";
            User result = userBus.AuthenticateAndRetrieveUser(email, "testing");
            Assert.IsNotNull(result, userBus.ErrorMessage);
            Assert.AreEqual(result.Username, email);
        }

        WeblogContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            var ctx = new WeblogContext(options);

            WeblogDataImporter.EnsureWeblogData(ctx,"server=.;database=Weblog;integrated security=true;");
            return ctx;

        }
    }
}
