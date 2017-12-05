using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Westwind.Weblog.Business;

namespace Westwind.Weblog.Controllers
{
    public class PostsController : Controller
    {
        private PostRepository PostRepo;

        public PostsController(PostRepository postRepo)
        {
            PostRepo = postRepo;
        }

        [Route("")]
        [Route("posts")]
        public IActionResult Index()
        {
            
            return View();
        }

        [Route("posts/{year:int}/{month:int}/{day:int}/{slug}")]
        public async Task<IActionResult> ShowPost(int year, int month, int day, string slug)
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