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
                                 .Where(p => p.Created > DateTime.Now.AddYears(-2))
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
            var postRepo = new PostBusiness(ctx, new Configuration.WeblogConfiguration());
            var post = await postRepo.GetPost(slug);

            Assert.IsNotNull(post);

            Console.WriteLine($"{post.Title} - {post.Markdown}");
        }


        [Test]
        public async Task GetPosts()
        {
            var config = new Configuration.WeblogConfiguration()
            {
                 PostPageSize = 10
            };
            var ctx = GetContext();
            var postRepo = new PostBusiness(ctx, config);

            var posts = await postRepo.GetLastPosts(config.PostPageSize);

            Assert.IsNotNull(posts);
            Assert.IsTrue(posts.Count > 0 && posts.Count <= config.PostPageSize);
            foreach(var post in posts)
                Console.WriteLine(post.Title);
        }


        [Test]
        public async Task GetRecentComments()
        {
            var config = new Configuration.WeblogConfiguration()
            {
                PostPageSize = 10
            };
            var ctx = GetContext();
            var postRepo = new PostBusiness(ctx, config);

            var comments = await postRepo.GetRecentComments(config.PostPageSize);

            Assert.IsNotNull(comments);
            Assert.IsTrue(comments.Count > 0 && comments.Count <= config.PostPageSize);
            foreach (var comment in comments)
                Console.WriteLine(comment.Title);
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
