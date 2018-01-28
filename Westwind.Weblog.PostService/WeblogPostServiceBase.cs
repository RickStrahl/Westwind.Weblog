using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Westwind.AspNetCore.Errors;
using Westwind.Weblog.PostService.Model;

namespace Westwind.Weblog.PostService
{
    [UnhandledApiExceptionFilter]
    public abstract class WeblogPostServiceBase : Controller
    {
        /// <summary>
        /// Optional internal value that holds the Authorization token
        /// passed in a Bearer token or 'token' query string
        /// </summary>
        protected string UserToken { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            UserToken = Request.Headers["Authorization"].FirstOrDefault();
            if (UserToken.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                UserToken = UserToken.Substring(7);
            if (string.IsNullOrEmpty(UserToken))
                UserToken = Request.Query["token"].FirstOrDefault();

            if (context.ActionDescriptor.DisplayName != "Authenticate" &&
                string.IsNullOrEmpty(UserToken))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            base.OnActionExecuting(context);
        }


        /// <summary>
        /// Authenticate a user and pass back a user token
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public abstract IActionResult Authenticate(string username, string password);

        
        /// <summary>
        /// Upload a new or updated blog post. If the post has a previous
        /// post Id it is assumed to be an existing post that is looked up.
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        public abstract string UploadPost([FromBody] Post post);

        /// <summary>
        /// Uploads a media object like an image or video to the server
        /// and returns a url if successful.
        /// </summary>
        /// <param name="media">a media object</param>
        /// <returns></returns>
        public abstract string UploadMediaObject([FromBody] MediaObject media);

        //public abstract string UploadRawMediaObject([FromBody] MediaObject media);


        /// <summary>
        /// Retrieves an initial blog post
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public abstract Post GetPost(string blogId, string postId);

        public abstract Post GetPosts(PostListFilter listFilter);
    }

    public class PostListFilter
    {
        public string BlogId { get; set; }

        public int NumberOfPosts { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
