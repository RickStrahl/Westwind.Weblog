using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class PostsController : Controller
    {
        PostBusiness PostRepo { get; }

        WeblogConfiguration Config  { get; }

        IMemoryCache Cache { get; }
        
        public PostsController(PostBusiness postRepo, 
                               WeblogConfiguration config,
                               IMemoryCache cache)
        {
            PostRepo = postRepo;
            Config = config;
            Cache = cache;
        }

        [Route("")]
        [Route("posts")]
        public async Task<IActionResult> Index()
        {
            var posts = await PostRepo.GetLastPosts(Config.HomePagePostCount);
            return View(new PostViewModel { Posts = posts, PostRepo = PostRepo });
        }

        //[Route("ShowPost.aspx?id={id:int}")]
        [Route("posts/{id:int}")]
        [Route("posts/{year:int}/{month}/{day:int}/{slug}")]
        public async Task<IActionResult> ShowPost(int year, string month, int day, string slug, int id=0)
        {
            Post post;
            if (id > 0)
                post = await PostRepo.GetPost(id);
            else
                post = await PostRepo.GetPost(slug);

            return View(new PostViewModel { Post = post, PostRepo = PostRepo });
        }

        [Route("comments")]
        public async Task<IActionResult> RecentComments()
        {
            var comments = await PostRepo.GetRecentComments(Config.HomePagePostCount);
            return View(new PostViewModel { Comments = comments, PostRepo = PostRepo});
        }
    
    }
}