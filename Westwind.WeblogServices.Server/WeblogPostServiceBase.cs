﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Westwind.AspNetCore.Errors;
using Westwind.WeblogPostService.Model;

namespace Westwind.WeblogServices.Server
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

            if (!string.IsNullOrEmpty(UserToken) && UserToken.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                UserToken = UserToken.Substring(7);

            if (string.IsNullOrEmpty(UserToken))
                UserToken = Request.Query["token"].FirstOrDefault();

            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (descriptor == null || 
                (descriptor.ActionName != "Authenticate" &&
                string.IsNullOrEmpty(UserToken)))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            base.OnActionExecuting(context);
        }


        /// <summary>
        /// Authenticate a user and pass back a user token
        /// </summary>
        /// <param name="getAuthRequest">Auth request with username and password</param>
        /// <returns></returns>
        public abstract string Authenticate(AuthenticateRequest getAuthRequest);

        
        /// <summary>
        /// Upload a new or updated blog post. If the post has a previous
        /// post Id it is assumed to be an existing post that is looked up.
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        public abstract string UploadPost([FromBody] WeblogPost post);

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
        /// <param name="getPostRequest">request parameter for blog id and post id</param>
        /// <returns></returns>
        public abstract WeblogPost GetPost( string userId, string blogId);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="listFilter"></param>
        /// <returns></returns>
        public abstract IList<WeblogMinimalPost> GetPosts(PostListFilter listFilter);
    }
}
