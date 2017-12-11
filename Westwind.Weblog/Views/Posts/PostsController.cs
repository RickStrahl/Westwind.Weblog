using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    public class PostsController : Controller
    {
        readonly PostRepository PostRepo;
        readonly WeblogConfiguration Config;
        

        public PostsController(PostRepository postRepo, WeblogConfiguration config)
        {
            PostRepo = postRepo;
            Config = config;
        }

        [Route("")]
        [Route("posts")]
        public async Task<IActionResult> Index()
        {
            var posts = await PostRepo.GetLastPosts(Config.HomePagePostCount);     
            return View(new PostViewModel { Posts=posts, PostRepo = PostRepo});
        }

        [Route("posts/{year:int}/{month}/{day:int}/{slug}")]
        public async Task<IActionResult> ShowPost(int year, string month, int day, string slug)
        {
            var post = await PostRepo.GetPost(slug);            
            return View(post);
        }

        //[Route("ShowPost.aspx?id={postId:int}")]
        //public IActionResult ShowPost(int postId)
        //{
        //    return View();
        //}
    }
}