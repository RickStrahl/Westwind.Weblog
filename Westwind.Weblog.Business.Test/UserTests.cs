using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Westwind.Weblog.Business.Models;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;
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
            ClassicAssert.IsNotNull(user);

            user.Password = "testing";

            var updatedUser = userBus.SaveUser(user);
            ClassicAssert.IsNotNull(updatedUser, userBus.ErrorMessage);

            Console.WriteLine(updatedUser.Password);
        }


        [Test]
        public void AuthenticateUserTest()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            bool result = userBus.AuthenticateUser("rstrahl@west-wind.com", "testing");
            ClassicAssert.IsTrue(result,userBus.ErrorMessage);            
        }


        [Test]
        public void AuthenticateUserFailTest()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            bool result = userBus.AuthenticateUser("rstrahl@west-wind.com", "Bogus");
            ClassicAssert.IsFalse(result, userBus.ErrorMessage);

            result = userBus.AuthenticateUser("rstrahl@west-wind.com", "");
            ClassicAssert.IsFalse(result, userBus.ErrorMessage);

            result = userBus.AuthenticateUser("rstrahl@west-wind.com", null);
            ClassicAssert.IsFalse(result, userBus.ErrorMessage);

            result = userBus.AuthenticateUser(null,"testing");
            ClassicAssert.IsFalse(result, userBus.ErrorMessage);
        }

        [Test]
        public void AuthenticateAndRetrieveUserTest()
        {
            var ctx = GetContext();
            var userBus = new UserBusiness(ctx, WeblogConfiguration.Current);

            string email = "rstrahl@west-wind.com";
            User result = userBus.AuthenticateAndRetrieveUser(email, "testing");
            ClassicAssert.IsNotNull(result, userBus.ErrorMessage);
            ClassicAssert.AreEqual(result.Username, email);
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
