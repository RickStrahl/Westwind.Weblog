using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    public class PostsController : Controller
    {
        PostRepository PostRepo { get; }

        WeblogConfiguration Config  { get; }
        
        public PostsController(PostRepository postRepo, 
                               WeblogConfiguration config,
                               IOptions<WeblogConfiguration> options)
        {
            PostRepo = postRepo;
            //Config = config;
            Config = options.Value;
        }

        [Route("")]
        [Route("posts")]
        public async Task<IActionResult> Index()
        {
            var posts = await PostRepo.GetLastPosts(Config.HomePagePostCount);
            return View(new PostViewModel { Posts = posts, PostRepo = PostRepo });
        }

        [Route("posts/{year:int}/{month}/{day:int}/{slug}")]
        public async Task<IActionResult> ShowPost(int year, string month, int day, string slug)
        {
            var post = await PostRepo.GetPost(slug);
            return View(new PostViewModel { Post = post, PostRepo = PostRepo });
        }

        //[Route("ShowPost.aspx?id={postId:int}")]
        //public IActionResult ShowPost(int postId)
        //{
        //    return View();
        //}
    }
}