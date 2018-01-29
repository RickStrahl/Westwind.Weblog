using System;
using Westwind.Utilities;
using Westwind.WeblogPostService.Model;

namespace Westwind.WeblogServices.Client
{
    public class WeblogPostServiceClient
    {
        public string AuthenticationToken { get; set; }


        /// <summary>
        /// An Api base url such as http://site.com/api 
        /// </summary>
        public string ApiBaseUrl { get; set; }



        /// <summary>
        /// Authenticates a user and returns a Bearer token
        /// Url: posts/authenticate
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="blogId"></param>
        /// <returns></returns>
        public string Authenticate(string username, string password, string blogId = null,string relativeUrl = "/posts/authenticate")
        {
            var data = new AuthenticateRequest
            {
                Username = username,
                Password = password,
                BlogId = blogId
            };

            var settings = new HttpRequestSettings
            {
                Content = data,
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };

            string token = null;
            try
            {
                token = HttpUtils.JsonRequest<string>(settings);
            }
            catch (Exception ex)
            {
                SetError("Failed to authenticate: " + ex.Message);
            }


            
            return token;
        }


        public string UploadPost(WeblogPost post, string relativeUrl = "/posts")
        {
            

            var settings = new HttpRequestSettings
            {
                Content = post,
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken}");

            string postId = null;
            try
            {
                postId = HttpUtils.JsonRequest<string>(settings);
            }
            catch (Exception ex)
            {
                SetError("Failed to send Post: " + ex.Message);
            }

            return postId;
        }

        public string UploadMediaObject(MediaObject image, string relativeUrl = "/posts/image")
        {
            var settings = new HttpRequestSettings
            {
                Content = image,
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken}");

            string imageUrl = null;
            try
            {
                imageUrl = HttpUtils.JsonRequest<string>(settings);
            }
            catch (Exception ex)
            {
                SetError("Failed to upload media object: " + ex.Message);
            }

            return imageUrl;
        }

        #region Errors

        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            this.SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                this.ErrorMessage = string.Empty;
                return;
            }
            this.ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                this.ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }
        #endregion
    }
}
