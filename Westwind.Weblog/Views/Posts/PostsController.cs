using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Westwind.Utilities;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class PostsController : AppBaseController
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
            var posts = await PostRepo.GetLastPostsAsync(Config.HomePagePostCount);
            return View(new PostViewModel { Posts = posts, PostRepo = PostRepo });
        }

        //[Route("ShowPost.aspx?id={id:int}")]
        [Route("posts/{id:int}")]
        [Route("posts/{year:int}/{month}/{day:int}/{slug}")]
        [Route("showpost.aspx")]        
        public async Task<IActionResult> ShowPost(int year, string month, int day, string slug, int id=0)
        {
            Post post;
            if (id > 0)
                post = await PostRepo.GetPost(id);
            else
                post = await PostRepo.GetPost(slug);

            // embed ads            
            post.Body = post.Body.Replace("##AD##", ""); // no Ads at the moment exclusive
            
            //var body = StringUtils.ReplaceStringInstance(post.Body, "##AD##", App.EmbeddedContentAd, 2, true);
            //body = StringUtils.ReplaceStringInstance(body, "##AD##", App.WestWindSquareAd, 3, true);
            //body = body.Replace("##AD##", App.EmbeddedContentAd);

            var page = Request.Query["page"].FirstOrDefault();
            int.TryParse(page, out int pageToDisplay);
            if (pageToDisplay < 1)
                pageToDisplay = 1;

            List<string> pages = new List<string>();
            if (post.Body.Contains("#PAGEBREAK"))
                pages = post.Body.Split(new[] {"#PAGEBREAK"}, StringSplitOptions.RemoveEmptyEntries).ToList();
            if(pages.Count < 1)
                pages.Add(post.Body);

            var totalPages = pages.Count;
            if (totalPages == 0)
                totalPages = 1;

            if (pageToDisplay <= totalPages)
                post.Body = pages[pageToDisplay - 1];
            else
                totalPages = 1;
            
            return View(new PostViewModel { Post = post, PostRepo = PostRepo, PageToDisplay = pageToDisplay, TotalPages = totalPages });
        }

        [Route("comments")]
        public async Task<IActionResult> RecentComments()
        {
            var comments = await PostRepo.GetRecentCommentsAsync(Config.HomePagePostCount);
            return View(new PostViewModel { Comments = comments, PostRepo = PostRepo});
        }
    
    }
}