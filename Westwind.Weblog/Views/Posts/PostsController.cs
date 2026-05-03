using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Westwind.AspNetCore.Markdown;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class PostsController : WeblogBaseController
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
        [Route("/posts")]
        public async Task<IActionResult> Index()
        {
            var posts = await PostRepo.GetLastPostsAsync(Config.HomePagePostCount);
            return View(new PostViewModel { Posts = posts, PostRepo = PostRepo });
        }



        //[Route("ShowPost.aspx?id={id:int}")]
        [HttpGet]
        [Route("/posts/{id}")]
        [Route("/posts/{year:int}/{month}/{day:int}/{slug}")]
        [Route("showpost.aspx")]        
        public async Task<IActionResult> ShowPost(int year, string month, int day, string slug, object html, string id= null)
        {
            Post post;
            if (!string.IsNullOrEmpty(id))
                post = await PostRepo.GetPost(id);
            else
                post = await PostRepo.GetPost(slug);

            if (post == null)
            {
                return Redirect("/");                
            }

            
            // Markdown
            string postHtml = post.BodyMode == 2 ? Markdown.Parse(post.Markdown) : post.Body; // html already rendered                       
            postHtml = PostRepo.EmbedAds(postHtml);

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
            
            var commentMessage = HttpContext.Items["CommentMessage"] as string; 
            if(!string.IsNullOrEmpty(commentMessage))
            {
                ErrorDisplay.ShowWarning(commentMessage);
            }

            return View(new PostViewModel { PostHtml = postHtml, Post = post, PostRepo = PostRepo, PageToDisplay = pageToDisplay, TotalPages = totalPages });
        }


        [HttpPost]
        [Route("/posts/{id}")]
        [Route("/posts/{year:int}/{month}/{day:int}/{slug}")]
        [Route("showpost.aspx")]
        public async Task<IActionResult> ShowPostPost([FromForm] PostViewModel model, [FromRoute] int year, [FromRoute] string month, [FromRoute] int day, [FromRoute] string slug, [FromRoute] string id = null)
        {

            Post post;           
            if(!string.IsNullOrEmpty(id))
                post = await PostRepo.GetPost(id);
            else
                post = await PostRepo.GetPost(slug);
            
            var page = Request.Query["page"].FirstOrDefault();
            int.TryParse(page, out int pageToDisplay);
            if (pageToDisplay < 1)
                pageToDisplay = 1;

            List<string> pages = new List<string>();
            if (post.Body.Contains("#PAGEBREAK"))
                pages = post.Body.Split(new[] { "#PAGEBREAK" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (pages.Count < 1)
                pages.Add(post.Body);

            var totalPages = pages.Count;
            if (totalPages == 0)
                totalPages = 1;

            if (pageToDisplay <= totalPages)
                post.Body = pages[pageToDisplay - 1];
            else
                totalPages = 1;

            // Markdown
            string postHtml = post.BodyMode == 2 ? Markdown.Parse(post.Markdown) : post.Body; // html already rendered                       
            postHtml = PostRepo.EmbedAds(postHtml);


            model.Post = post;
            model.PostRepo = PostRepo;

            var newModel = new PostViewModel { PostHtml = postHtml, Post = post, ActiveComment = model.ActiveComment, PostRepo = PostRepo, PageToDisplay = pageToDisplay, TotalPages = totalPages };
            var comment = newModel.ActiveComment;


            comment.IsCommentDialogVisible = true;
            comment.Post= post;
            comment.CommentErrorMessage = HttpContext.Items["CommentMessage"]?.ToString();

            InitializeViewModel(newModel);
            
            // posting back
            if (!string.IsNullOrEmpty(comment.CommentText))
            {
                
                var dataComment = PostRepo.Create<Comment>();
                dataComment.Body = comment.CommentText;
                dataComment.Title = "re: " + comment.Post.Title;
                dataComment.Author = comment.CommentAuthor;
                dataComment.Email = comment.CommentEmail;
                
                // TODO: Make this False and manually require enabling
                dataComment.IsActive = false;
                
                post.Comments.Add(dataComment);
                
                if(await PostRepo.SaveAsync(post))
                {
                    ModelState.Clear();
                    HttpContext.Items["CommentMessage"] = "Comment has been saved, but comment moderation is enabled, so it won't display until approved. Please check back later.";
                    return Redirect(Request.Path.Value.Contains("#Comments") ? string.Empty : "#Comments");                    
                }

                
                comment.CommentErrorMessage = $"Failed to save comment: {PostRepo.ErrorMessage}";
            }
            
            return View("ShowPost",newModel);
        }

        [Route("/comments")]
        public async Task<IActionResult> RecentComments()
        {
            var comments = await PostRepo.GetRecentCommentsAsync(Config.HomePagePostCount);
            var model = new PostViewModel { Comments = comments, PostRepo = PostRepo };
            InitializeViewModel(model);
            return View(model);
        }
    
    }
}