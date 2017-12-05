using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Westwind.Weblog.Business.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Westwind.Weblog.Business.Test
{
    [TestFixture]
    public class PostTests
    {
        public string ConnectionString = "server=.;database=WeblogCore;integrated security=true;";

        [Test]
        public void GetDbContextRecentPostsWithCommentsTest()
        {
            var ctx = GetContext();

            var posts = ctx.Posts.Include("Comments")
                                 .Where(p => p.Entered > DateTime.Now.AddYears(-2))
                                 .ToList();

            foreach (var post in posts)
            {
                Console.WriteLine($"{post.Title} - {post.Comments.Count} - {post.SafeTitle}");
            }
            Console.WriteLine(posts.Count);
        }

        [Test]
        public async Task GetPostBySlugTest()
        {
            var slug = "ASPNET-Core-and-CORS-Gotchas";

            var ctx = GetContext();
            var postRepo = new PostRepository(ctx);
            var post = await postRepo.GetPost(slug);

            Assert.IsNotNull(post);

            Console.WriteLine($"{post.Title} - {post.Markdown}");
        }


        WeblogContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            var ctx = new WeblogContext(options);

            WeblogDataImporter.EnsureWeblogData(ctx);
            return ctx;

        }
    }
}
