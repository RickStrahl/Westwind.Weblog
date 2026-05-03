using System;
using System.Threading.Tasks;
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
        public async Task<string> Authenticate(string username, string password, string blogId = null,string relativeUrl = "/posts/authenticate")
        {
            var data = new AuthenticateRequest
            {
                Username = username,
                Password = password,
                BlogId = blogId
            };

            var settings = new HttpClientRequestSettings
            {                 
                RequestContent = data,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };

            string token = null;
            try
            {
                token = await HttpClientUtils.DownloadJsonAsync<string>(settings);
            }
            catch (Exception ex)
            {
                SetError("Failed to authenticate: " + ex.Message);
            }


            
            return token;
        }


        public async Task<string> UploadPost(WeblogPost post, string relativeUrl = "/posts")
        {
            

            var settings = new HttpClientRequestSettings
            {
                RequestContent = post,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken}");

            string postId = null;
            try
            {
                postId = await HttpClientUtils.DownloadJsonAsync<string>(settings);
            }
            catch (Exception ex)
            {
                SetError("Failed to send Post: " + ex.Message);
            }

            return postId;
        }

        public async Task<string> UploadMediaObject(MediaObject image, string relativeUrl = "/posts/image")
        {
            var settings = new HttpClientRequestSettings
            {
                RequestContent = image,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken}");

            string imageUrl = null;
            try
            {
                imageUrl = await HttpClientUtils.DownloadJsonAsync<string>(settings);
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
