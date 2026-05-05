using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Westwind.AspNetCore;
using Westwind.AspNetCore.Errors;
using Westwind.Utilities.Data.Security;
using Westwind.WeblogPostService.Model;

namespace Westwind.WeblogServices.Server
{


    public class WeblogTokenInfo
    {
        public static int TokenTimeoutSeconds = 30 * 60;

        public string Token { get; set; }
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddSeconds(TokenTimeoutSeconds);

    }


    public abstract class WeblogPostServiceBase : BaseApiController
    {
        /// <summary>
        /// Optional internal value that holds the Authorization token
        /// passed in a Bearer token or 'token' query string
        /// </summary>
        protected string UserToken { get; set; }

        protected string ConnectionString { get; set;  }

        public WeblogPostServiceBase(string connectionString) 
        {
            ConnectionString = connectionString;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            UserToken = Request.Headers.Authorization;
            if (string.IsNullOrEmpty(UserToken))
                UserToken = Request.Query["token"].FirstOrDefault() ?? string.Empty;
            
            if (UserToken.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                UserToken = UserToken.Substring(7);

            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor == null)
            {
                throw new ApiException("Invalid publish url");
            }
           
            if (!descriptor.ActionName.Equals("authenticate", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(UserToken))
                {
                    throw new ApiException("You're not authorized to access this request. Missing Authorization token.", 401);
                }

                var tm = new UserTokenManager(ConnectionString);
                tm.TokenTimeoutSeconds = WeblogTokenInfo.TokenTimeoutSeconds;
                if (!tm.IsTokenValid(UserToken))
                {
                    throw new ApiException("Invalid or expired authorization token.", 401);
                }
            }


            base.OnActionExecuting(context);
        }



        /// <summary>
        /// Creates a new User Token
        /// </summary>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public string CreateNewToken(string userToken = null)
        {
            var tm = new UserTokenManager(ConnectionString);
            tm.TokenTimeoutSeconds = WeblogTokenInfo.TokenTimeoutSeconds;
            var token = tm.CreateNewToken(userToken);
            if (token == null)
            {
                throw new ApiException("Unable to create a new user token");
            }
            return token;
        }

        /// <summary>
        /// Checks to see if a user token is valid
        /// </summary>
        /// <param name="userToken">token to check if it's valid</param>
        /// <returns></returns>
        public bool ValidateUserToken(string userToken)
        {
            var tm = new UserTokenManager(ConnectionString);
            tm.TokenTimeoutSeconds = WeblogTokenInfo.TokenTimeoutSeconds;
            return tm.IsTokenValid(userToken);
        }


        /// <summary>
        /// Authenticate a user and pass back a user token via a WeblogTokenInfo object.
        /// Call `CreateNewToken` to generate the actual token to assign.
        /// </summary>
        /// <param name="getAuthRequest">Auth request with username and password</param>
        /// <returns>WeblogTokenInfo that you create with a token parameter</returns>
        public abstract WeblogTokenInfo Authenticate(AuthenticateRequest getAuthRequest);


        /// <summary>
        /// Upload a new or updated blog post. If the post has a previous
        /// post Id it is assumed to be an existing post that is looked up.
        /// </summary>
        /// <param name="post"></param>
        /// <param name="blogId"></param>
        /// <returns></returns>
        public abstract WeblogPost UploadPost([FromBody] WeblogPost post);

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
        public abstract IList<WeblogMinimalPost> GetRecentPosts(PostListFilter listFilter);
    }
}
