
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Westwind.Utilities;
using Westwind.WeblogPostService.Model;
using Westwind.WeblogServices.Client;


namespace Westwind.Weblog.PostServiceTests
{
    [TestFixture]
    public class ServiceClientTests
    {
        private string ServiceUrl = "http://localhost:5004/api";

        [Test]
        public void AuthenticateTest()
        {
            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl
            };
            var token = client.Authenticate("rstrahl@west-wind.com", "testing");

            Assert.IsNotEmpty(token);
            Console.WriteLine(token);

        }

        [Test]
        public void FailAuthenticateTest()
        {
            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl
            };

            var token = client.Authenticate("rstrahl@west-wind.com", "ding");
            

            Assert.IsNotEmpty(token);
            Console.WriteLine(token);

        }


        [Test]
        public void NewWeblogPostTest()
        {

            var post = new WeblogPost()
            {
                PostId = "1683236",
                Title = "UPDATED! A new Test Post",
                Body = "This is a long post with pointless points.",
                Abstract = "This is an abstracted abstract that's just as pointless - and longer.",
                Author = "Rick Strahl",
                DateCreated = DateTime.Now,
                RawPostText = "Markdown Text goes **here**." ,
                Location = "Paia, HI",
                ImageUrl = "http://localhost:5004/images/RickHero1.jpg", 
                Keywords = "long,post,pointless"
            };
            post.Categories = "Life., .NET, ASP.NET";

            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl
            };
            var token = client.Authenticate("rstrahl@west-wind.com", "testing");
            
            Assert.IsNotNull(token);
            var postId = client.UploadPost(post);

            Assert.IsNotNull(postId);
            Assert.IsNotEmpty(postId);
        }
    }
}
