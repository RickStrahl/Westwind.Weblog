using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Westwind.Utilities;
using Westwind.WeblogPostService.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Westwind.WeblogServices.Client
{
    public class WeblogPostServiceClient
    {
        public WeblogTokenInfo AuthenticationToken { get; set; }


        /// <summary>
        /// An Api base url such as http://site.com/api 
        /// </summary>
        public string ApiBaseUrl
        {
            get => field?.TrimEnd('/');
            set;
        }


        public string LastRequestContent { get; set; }

        public string LastResponseContent { get; set; }


        /// <summary>
        /// Authenticates a user and returns a Bearer token
        /// Url: posts/authenticate
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="blogId"></param>
        /// <returns></returns>
        public async Task<WeblogTokenInfo> Authenticate(string username, string password, string blogId = null,string relativeUrl = "publish/authenticate")
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
                Url = ApiBaseUrl.TrimEnd('/') + "/" + relativeUrl.Trim('/'),
                HttpVerb = "POST"          ,
                CaptureRequestAndResponse = true
            };

            WeblogTokenInfo token = null;
            try
            {
                token = await HttpClientUtils.DownloadJsonAsync<WeblogTokenInfo>(settings);
                if (token == null)
                {
                    SetError("Failed to authenticate: " + settings.CapturedResponseContent);
                    ParseJsonError(settings, "Failed to authenticate.");
                }

            }
            catch (Exception ex)
            {
                
                ParseJsonError(settings, "Failed to authenticate",ex);
            }

            AuthenticationToken = token;

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return token;
        }


        public async Task<WeblogPost> GetPost(string postId, string blogId = null, string relativeUrl = "publish")
        {
            EnsureAuthToken();

            var settings = new HttpClientRequestSettings
            {                
                Url  = ApiBaseUrl.TrimEnd('/') + "/" + relativeUrl.Trim('/') + "/" + postId,
                HttpVerb = "POST",
                CaptureRequestAndResponse = true,                
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            WeblogPost post = null;

            Console.WriteLine(settings.Url);

            try
            {
                post = await HttpClientUtils.DownloadJsonAsync<WeblogPost>(settings);
                if (post == null)
                {
                    ParseJsonError(settings, "Couldn't retrieve post.");
                }

            }
            catch (Exception ex)
            {                
                ParseJsonError(settings, "Couldn't retrieve post.", ex);
            }
            
            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return post;
        }

        public async Task<WeblogPost> UploadPost(WeblogPost post, string relativeUrl = "publish")
        {
            EnsureAuthToken();

            var settings = new HttpClientRequestSettings
            {
                RequestContent = post,
                RequestContentType = "application/json",
                Url = ApiBaseUrl.TrimEnd('/') + "/" + relativeUrl.Trim('/'),
                HttpVerb = "POST",
                CaptureRequestAndResponse = true
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            post = null;
            try
            {
                post = await HttpClientUtils.DownloadJsonAsync<WeblogPost>(settings);
                if (post == null)
                {
                    ParseJsonError(settings, "Failed to publish new post.");
                }

            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Failed to publish new post.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return post;
        }

        public async Task<string> UploadMediaObject(MediaObject image, string relativeUrl = "/posts/image")
        {
            EnsureAuthToken();

            var settings = new HttpClientRequestSettings
            {
                RequestContent = image,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + relativeUrl,
                HttpVerb = "POST"
            };
            if (AuthenticationToken != null)
                settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken.Token}");

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

        /// <summary>
        /// Checks to see if the Authorization token has been set is not expired.
        /// If not throws an exception.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected void EnsureAuthToken()
        {
            if (string.IsNullOrEmpty(AuthenticationToken?.Token))
            {
                throw new UnauthorizedAccessException("Please make sure to call Authenticate before making this request.");
            }
            if (AuthenticationToken.ExpirationUtc < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Authentication token has expired, please call Authenticate again.");
            }            
        }

        /// <summary>
        /// Parses out an error message from Json if it exists.
        /// 
        /// Looks for `message` property in the returned JSON content.
        /// </summary>
        /// <param name="settings">HttpClientSettings instance from re quest</param>
        /// <param name="baseMessage">A base error message that if provided is pre-pended to any retrieve error message.</param>
        /// <param name="ex">Optional exception that is used if there's no error content</param>
        protected void ParseJsonError(HttpClientRequestSettings settings, string baseMessage = null, Exception ex = null )
        {
            
            if (settings == null)
                return;

            SetError();

            string errorMessage = null;

            if (!string.IsNullOrEmpty(settings.CapturedResponseContent) &&
                settings.Response?.Content != null && settings.CapturedResponseContent.Trim().StartsWith("{"))
            {
                var error = JsonSerializationUtils.Deserialize<JObject>(settings.CapturedResponseContent);
                if (error != null)
                {
                     errorMessage = error["message"]?.ToString();
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        SetError((!string.IsNullOrEmpty(baseMessage) ? $"{baseMessage}: " : "") +  errorMessage);
                    }
                }
            }

            if (string.IsNullOrEmpty(errorMessage))            
            {                
                if (ex != null)
                    baseMessage += $": {ex.Message}";
                SetError(baseMessage);
            }
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



    public class WeblogTokenInfo
    {
        public static int TokenTimeoutSeconds = 30 * 60;

        public string Token { get; set; }
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddSeconds(TokenTimeoutSeconds);


        public override string ToString()
        {
            return Token;
        }
    }

}
