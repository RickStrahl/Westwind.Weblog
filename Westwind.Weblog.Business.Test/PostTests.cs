using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Westwind.Weblog.Business.Models;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;

namespace Westwind.Weblog.Business.Test
{
    [TestFixture]
    public class PostTests
    {
        public string ConnectionString = "server=.;database=WeblogCore;integrated security=true;encrypt=false";

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

            ClassicAssert.IsNotNull(post);

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

            var posts = await postRepo.GetLastPostsAsync(config.PostPageSize);

            ClassicAssert.IsNotNull(posts);
            ClassicAssert.IsTrue(posts.Count > 0 && posts.Count <= config.PostPageSize);
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

            var comments = await postRepo.GetRecentCommentsAsync(config.PostPageSize);

            ClassicAssert.IsNotNull(comments);
            ClassicAssert.IsTrue(comments.Count > 0 && comments.Count <= config.PostPageSize);
            foreach (var comment in comments)
                Console.WriteLine(comment.Title);
        }


        [Test]
        public async Task PostSearchTests()
        {
            var config = new Configuration.WeblogConfiguration()
            {
                PostPageSize = 10
            };
            var ctx = GetContext();
            var postRepo = new PostBusiness(ctx, config);

            var search = "Westwind.Scripting";

            var posts = await postRepo.PostSearchAsync(search, 15);

            

            ClassicAssert.IsNotNull(posts);
            ClassicAssert.IsTrue(posts.Count > 0 && posts.Count <= config.PostPageSize);
            foreach (var post in posts)
                Console.WriteLine(post.Title);
        }

        [Test]
        public void GetRelatedPostsTest()
        {
            var config = new Configuration.WeblogConfiguration()
            {
                PostPageSize = 10
            };
            var ctx = GetContext();
            var postRepo = new PostBusiness(ctx, config);

            var pl = postRepo.GetRelatedPosts(["ASP.NET",".NET"], 5, "asddas");

            ClassicAssert.IsNotNull(pl);

            foreach(var p in pl)
            {
                Console.WriteLine(p.PostId + " " + p.Title + "\n\t" + p.SafeTitle);
            }
        }

        [Test]
        public void GetCategoriesTest()
        {
            var ctx = GetContext();
            var repo = new PostBusiness(ctx,  new Configuration.WeblogConfiguration());

            var cats = repo.GetCategories();

            foreach(var cat in cats)
                Console.WriteLine($"{cat.Key}: {cat.Value}");

            
        }

        WeblogContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WeblogContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            var ctx = new WeblogContext(options);

            WeblogDataImporter.EnsureWeblogData(ctx,ConnectionString);
            return ctx;

        }
    }
}
