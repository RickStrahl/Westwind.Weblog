
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
        private string ServiceUrl = "https://localhost:5001/api";

        private const string Username = "test@test.com";
        private const string Password = "test";

        [Test]
        public async Task AuthenticateTest()
        {
            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl,                   
            };
            var token = await client.Authenticate(Username, Password);

            Assert.IsNotNull(token, client.ErrorMessage);
            Console.WriteLine(token.Token);

        }

        [Test]
        public async Task FailAuthenticateTest()
        {
            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl,
            };
            var token = await client.Authenticate(Username, Password);

            Assert.IsNotNull(token, client.ErrorMessage);
            Console.WriteLine(token.Token);

        }


        [Test]
        public async Task GetPostTest()
        {
            string postId = "5314605";

            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl 
            };

            var token = await client.Authenticate(Username, Password);
            Assert.IsNotNull(token, client.ErrorMessage);                        

            var post = await client.GetPost(postId);

            Assert.IsNotNull(post, client.ErrorMessage);
            Assert.AreEqual(postId, post.PostId);
            Assert.IsNotEmpty(post.Body);

            Console.WriteLine(post.Title);
            Console.WriteLine(client.LastResponseContent);
        }

        [Test]
        public async Task NewWeblogPostTest()
        {

            var post = new WeblogPost()
            {                
                Title = $"New Post! {DataUtils.GenerateUniqueId(6)} A new Test Post ",
                Body = "This is a <b>long post</b> with pointless points.",
                Abstract = "This is an abstracted abstract that's just as pointless - and longer.",
                Author = "Rick Strahl",
                DateCreated = DateTime.Now,
                RawPostText = "his is a **long post** with pointless points.",
                Location = "Paia, HI",
                ImageUrl = "http://localhost:5001/images/RickHero1.jpg", 
                Keywords = "long,post,pointless"
            };
            post.Categories = ["Life", ".NET", "ASP.NET"];
            post.CustomFields.Add("mt_GithubUrl", "https://github.com/rickstrahl/imagedrop");

            var client = new WeblogPostServiceClient()
            {
                ApiBaseUrl = ServiceUrl
            };

            var token = await client.Authenticate(Username, Password);
            Assert.IsNotNull(token, client.ErrorMessage);


            Assert.IsNotNull(token);
            Console.WriteLine(token.Token);


            post = await client.UploadPost(post);

            Assert.IsNotNull(post, client.ErrorMessage);
            
        }
    }
}
